import assert from "node:assert/strict";
import { EventEmitter } from "node:events";
import test from "node:test";
import {
  composeConnection,
  prepareEnvironment,
  stopProcessTree,
  supervise,
  main,
} from "./run-project.mjs";

const composeJson = JSON.stringify({
  services: {
    mysql: {
      ports: [{ target: 3306, published: "3307" }],
      environment: {
        MYSQL_DATABASE: "workshops",
        MYSQL_USER: "workshops",
        MYSQL_PASSWORD: 'a;"b$ c',
      },
    },
  },
});

class FakeCommands {
  calls = [];
  failure = false;
  failOnCall = 0;
  execute = (command, args, options) => {
    this.calls.push({ command, args, options });
    return {
      status: this.failure || this.calls.length === this.failOnCall ? 1 : 0,
      stdout: composeJson,
    };
  };
}

class FakeProcesses {
  children = [];
  stopped = [];
  signals = new EventEmitter();
  launch = (command, args, options) => {
    const child = new EventEmitter();
    Object.assign(child, {
      command,
      args,
      options,
      pid: this.children.length + 100,
    });
    this.children.push(child);
    return child;
  };
  stop = (child) => {
    this.stopped.push(child.pid);
  };
}

test("MySQL quotes passwords and uses the published Compose port", () => {
  assert.equal(
    composeConnection(composeJson),
    'Server=127.0.0.1;Port="3307";Database="workshops";User="workshops";Password="a;""b$ c"',
  );
  assert.throws(() => composeConnection("{}"), /Configuracao MySQL incompleta/);
});

test("InMemory needs no Docker and does not mutate inherited configuration", () => {
  const commands = new FakeCommands();
  const inherited = { DOTNET_ENVIRONMENT: "Production" };
  const environment = prepareEnvironment(
    "InMemory",
    commands.execute,
    inherited,
  );
  assert.equal(environment.DOTNET_ENVIRONMENT, "Development");
  assert.equal(inherited.DOTNET_ENVIRONMENT, "Production");
  assert.deepEqual(commands.calls, []);
  assert.throws(
    () => prepareEnvironment("invalid", commands.execute),
    /invalid/,
  );
});

test("MySQL waits for health and passes credentials only in the API environment", () => {
  const commands = new FakeCommands();
  const environment = prepareEnvironment("MySql", commands.execute, {});
  assert.equal(
    environment.ConnectionStrings__Workshops,
    composeConnection(composeJson),
  );
  assert.ok(commands.calls[1].args.includes("--wait"));
  assert.ok(
    commands.calls.every(
      (call) => !call.args.some((arg) => arg.includes("a;")),
    ),
  );
  commands.failure = true;
  assert.throws(
    () => prepareEnvironment("MySql", commands.execute),
    /Compose indisponivel/,
  );
});

test("a failed server stops its sibling and returns its error code", async () => {
  const processes = new FakeProcesses();
  const finished = supervise(
    "MySql",
    { ConnectionStrings__Workshops: "private" },
    processes.launch,
    processes.stop,
    processes.signals,
  );
  assert.ok(
    processes.children[0].args.includes("--Persistence:Provider=MySql"),
  );
  assert.equal(
    processes.children[0].options.env.ConnectionStrings__Workshops,
    "private",
  );
  assert.notEqual(
    processes.children[1].options.env.ConnectionStrings__Workshops,
    "private",
  );
  processes.children[0].emit("exit", 5);
  assert.equal(await finished, 5);
  assert.deepEqual(processes.stopped, [100, 101]);
  assert.equal(processes.signals.listenerCount("SIGINT"), 0);
});

test("Ctrl+C stops both trees once and returns success", async () => {
  const processes = new FakeProcesses();
  const finished = supervise(
    "InMemory",
    {},
    processes.launch,
    processes.stop,
    processes.signals,
  );
  processes.signals.emit("SIGINT");
  processes.children[0].emit("exit", 1);
  assert.equal(await finished, 0);
  assert.deepEqual(processes.stopped, [100, 101]);
});

test("cleanup targets only owned process trees on Windows and Unix", () => {
  const commands = new FakeCommands();
  stopProcessTree({ pid: 123 }, "win32", commands.execute);
  assert.deepEqual(commands.calls[0].args, ["/PID", "123", "/T", "/F"]);
  const signals = [];
  stopProcessTree({ pid: 456 }, "linux", commands.execute, (...args) =>
    signals.push(args),
  );
  assert.deepEqual(signals, [[-456, "SIGTERM"]]);
  stopProcessTree({}, "win32", commands.execute);
  assert.equal(commands.calls.length, 1);
});

test("invalid Compose output does not expose its contents", () => {
  assert.throws(
    () => composeConnection("secret-password"),
    (error) => !error.message.includes("secret-password"),
  );
});

test("failed MySQL healthcheck prevents application startup", () => {
  const commands = new FakeCommands();
  commands.failOnCall = 2;
  assert.throws(
    () => prepareEnvironment("MySql", commands.execute),
    /MySQL nao iniciou/,
  );
});

test("entry point rejects unknown providers before starting servers", async () => {
  await assert.rejects(main("invalid"), /Provider recebido/);
});

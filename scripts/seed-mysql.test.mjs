import assert from "node:assert/strict";
import test from "node:test";
import { seedMySql } from "./seed-mysql.mjs";

class FakeSeedCommands {
  calls = [];
  status = 0;
  execute = (command, args, options) => {
    this.calls.push({ command, args, options });
    return {
      status: args.includes("exec") ? this.status : 0,
      stdout: JSON.stringify({
        services: {
          mysql: {
            ports: [{ target: 3306, published: "3306" }],
            environment: {
              MYSQL_DATABASE: "workshops",
              MYSQL_USER: "workshops",
              MYSQL_PASSWORD: "private-value",
            },
          },
        },
      }),
    };
  };
}

test("seed waits for MySQL and sends SQL on stdin without exposing credentials", () => {
  const commands = new FakeSeedCommands();
  seedMySql(commands.execute);
  assert.ok(commands.calls[1].args.includes("--wait"));
  const invocation = commands.calls[2];
  assert.ok(invocation.args.includes("-T"));
  assert.match(invocation.options.input.toString(), /START TRANSACTION/);
  assert.match(invocation.options.input.toString(), /COMMIT/);
  assert.ok(
    commands.calls.every(
      (call) => !call.args.join(" ").includes("private-value"),
    ),
  );
});

test("seed reports SQL failure instead of declaring success", () => {
  const commands = new FakeSeedCommands();
  commands.status = 1;
  assert.throws(() => seedMySql(commands.execute), /Seed MySQL falhou/);
});

test("seed summarizes the expanded examples after successful execution", () => {
  const commands = new FakeSeedCommands();
  assert.equal(
    seedMySql(commands.execute),
    "Seed MySQL concluido.",
  );
});

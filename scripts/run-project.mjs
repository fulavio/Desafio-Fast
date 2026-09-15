import { spawn, spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

export const projectRoot = dirname(dirname(fileURLToPath(import.meta.url)));

/** Builds a quoted connection string from Compose JSON; e.g. a password containing semicolons. */
export function composeConnection(serialized) {
  let configuration;
  try {
    configuration = JSON.parse(serialized);
  } catch {
    throw new Error(
      "Resposta Compose invalida; esperado JSON de docker compose config.",
    );
  }
  const mysql = configuration.services?.mysql;
  const port = mysql?.ports?.find((entry) => entry.target === 3306)?.published;
  const settings = mysql?.environment;
  if (
    !port ||
    !settings?.MYSQL_PASSWORD ||
    !settings?.MYSQL_USER ||
    !settings?.MYSQL_DATABASE
  ) {
    throw new Error(
      "Configuracao MySQL incompleta; esperados porta 3306 publicada, usuario, senha e banco no Compose.",
    );
  }
  const quote = (value) => `"${String(value).replaceAll('"', '""')}"`;
  return `Server=127.0.0.1;Port=${quote(port)};Database=${quote(settings.MYSQL_DATABASE)};User=${quote(settings.MYSQL_USER)};Password=${quote(settings.MYSQL_PASSWORD)}`;
}

/** Resolves local MySQL settings and waits for its healthcheck; e.g. provider MySql. */
export function prepareEnvironment(
  provider,
  execute = spawnSync,
  inherited = process.env,
) {
  if (!["MySql", "InMemory"].includes(provider)) {
    throw new Error(
      `Provider recebido '${provider}'; esperado MySql ou InMemory.`,
    );
  }
  const environment = {
    ...inherited,
    ASPNETCORE_ENVIRONMENT: "Development",
    DOTNET_ENVIRONMENT: "Development",
  };
  if (provider === "InMemory") return environment;
  const compose = [
    "compose",
    "--project-directory",
    projectRoot,
    "-f",
    join(projectRoot, "compose.yaml"),
  ];
  const configuration = execute(
    "docker",
    [...compose, "config", "--format", "json"],
    { encoding: "utf8", windowsHide: true },
  );
  if (configuration.error || configuration.status !== 0)
    throw new Error(
      "Compose indisponivel ou .env invalido; esperado Docker instalado e senhas configuradas conforme .env.example.",
    );
  environment.ConnectionStrings__Workshops = composeConnection(
    configuration.stdout,
  );
  const startup = execute(
    "docker",
    [...compose, "up", "-d", "--wait", "--wait-timeout", "180", "mysql"],
    { stdio: "inherit", windowsHide: true },
  );
  if (startup.error || startup.status !== 0)
    throw new Error(
      "MySQL nao iniciou; esperado Docker ativo e healthcheck aprovado.",
    );
  return environment;
}

/** Stops only the launched process tree; e.g. dotnet run and its API child. */
export function stopProcessTree(
  child,
  platform = process.platform,
  execute = spawnSync,
  signal = process.kill,
) {
  if (!child.pid) return;
  if (platform === "win32") {
    execute("taskkill", ["/PID", String(child.pid), "/T", "/F"], {
      stdio: "ignore",
      windowsHide: true,
    });
    return;
  }
  try {
    signal(-child.pid, "SIGTERM");
  } catch (error) {
    if (error.code !== "ESRCH") throw error;
  }
}

/** Supervises API and Angular until Ctrl+C or either exits; e.g. InMemory development. */
export function supervise(
  provider,
  environment,
  launch = spawn,
  stop = stopProcessTree,
  signals = process,
) {
  const children = [];
  let finished = false;
  return new Promise((resolve) => {
    const finish = (code) => {
      if (finished) return;
      finished = true;
      signals.removeListener("SIGINT", interrupt);
      signals.removeListener("SIGTERM", interrupt);
      children.forEach((child) => stop(child));
      resolve(code);
    };
    const interrupt = () => finish(0);
    signals.once("SIGINT", interrupt);
    signals.once("SIGTERM", interrupt);
    const commands = [
      [
        "dotnet",
        [
          "run",
          "--project",
          join(projectRoot, "backend/Fast.Workshops.Api"),
          "--no-launch-profile",
          "--",
          "--urls=http://localhost:5000",
          `--Persistence:Provider=${provider}`,
        ],
        environment,
      ],
      [
        process.execPath,
        [
          join(projectRoot, "frontend/node_modules/@angular/cli/bin/ng.js"),
          "serve",
          "--host",
          "localhost",
          "--port",
          "4200",
        ],
        { ...process.env, CI: "true" },
      ],
    ];
    for (const [command, args, env] of commands) {
      const child = launch(command, args, {
        cwd: command === "dotnet" ? projectRoot : join(projectRoot, "frontend"),
        env,
        stdio: "inherit",
        windowsHide: true,
        detached: process.platform !== "win32",
      });
      children.push(child);
      child.once("error", () => {
        console.error(
          `Falha ao executar '${command}'; confira o setup e o PATH.`,
        );
        finish(1);
      });
      child.once("exit", (code) => finish(code || 1));
    }
  });
}

/** Starts both applications after setup; e.g. node scripts/run-project.mjs InMemory. */
export async function main(provider) {
  if (
    !existsSync(
      join(projectRoot, "frontend/node_modules/@angular/cli/bin/ng.js"),
    )
  ) {
    throw new Error(
      "Angular CLI local ausente; execute scripts/setup.sh ou scripts/setup.ps1.",
    );
  }
  const environment = prepareEnvironment(provider);
  console.log(
    `Iniciando ${provider}: frontend http://localhost:4200 | API http://localhost:5000 | Ctrl+C para encerrar.`,
  );
  return supervise(provider, environment);
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  try {
    process.exitCode = await main(process.argv[2]);
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}

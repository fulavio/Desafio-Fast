import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { prepareEnvironment, projectRoot } from "./run-project.mjs";

const seedArguments = [
  "compose",
  "--project-directory",
  projectRoot,
  "-f",
  join(projectRoot, "compose.yaml"),
  "exec",
  "-T",
  "mysql",
  "sh",
  "-c",
  'export MYSQL_PWD="$MYSQL_PASSWORD"; exec mysql --default-character-set=utf8mb4 -u "$MYSQL_USER" "$MYSQL_DATABASE"',
];

/** Seeds the local Compose database explicitly; e.g. node scripts/seed-mysql.mjs.
 * @param {typeof spawnSync} execute
 * @returns {string} Summary of the examples ensured by the seed.
 */
export function seedMySql(execute = spawnSync) {
  prepareEnvironment("MySql", execute);
  const result = execute("docker", seedArguments, {
    input: readFileSync(join(projectRoot, "scripts/seed-mysql.sql")),
    stdio: ["pipe", "inherit", "inherit"],
    windowsHide: true,
  });
  if (result.error || result.status !== 0)
    throw new Error(
      "Seed MySQL falhou; esperado schema inicializado e conexao disponivel.",
    );
  return "Seed MySQL concluido: 20 workshops trimestrais (2022–2026), 30 colaboradores, 20 atas e 480 participações de exemplo disponíveis. Registros anteriores foram preservados.";
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  try {
    console.log(seedMySql());
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}

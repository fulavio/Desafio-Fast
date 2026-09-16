# FAST Workshops

Aplicação FullStack para cadastrar workshops, colaboradores e atas de presença e consultar a participação nos workshops trimestrais da FAST Soluções.

## Arquitetura

O backend é um único projeto ASP.NET Core MVC. Não utiliza Clean Architecture. O Angular fornece a interface web e consome a API JSON.

```mermaid
flowchart LR
    UI["Angular"] --> C["Controllers"]
    C --> S["Services"]
    S --> R["Repository interfaces"]
    R --> M["Repositories em memória"]
    R --> DB["Repositories MySQL"]
```

Responsabilidades e estrutura completa: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Pré-requisitos

- SDK .NET 10 LTS (`global.json` aceita os feature bands instalados de 10.0);
- Node.js 24 (`frontend/.nvmrc` e `frontend/package.json`);
- npm;
- Bash ou, no Windows, PowerShell 5.1 ou superior para os scripts de automação.

O modo memória não exige banco ou credenciais. Para MySQL e validação completa, instale Docker com containers Linux e mantenha o daemon em execução.

## Setup

Prepare as dependências de maneira idempotente:

```bash
./scripts/setup.sh
```

No Windows, execute no PowerShell:

```powershell
.\scripts\setup.ps1
```

O comando pode ser executado novamente sem exigir limpeza manual.

## Execução

Após o setup, inicie backend e frontend juntos em um terminal:

| Modo       | PowerShell (Windows)         | Bash                             |
| ---------- | ---------------------------- | -------------------------------- |
| Em memória | `.\scripts\run-inmemory.ps1` | `bash ./scripts/run-inmemory.sh` |
| MySQL      | `.\scripts\run-mysql.ps1`    | `bash ./scripts/run-mysql.sh`    |

Para MySQL, prepare `.env` conforme `.env.example` e mantenha o Docker ativo. O script inicia o serviço `mysql` e aguarda seu healthcheck. Ele usa a configuração resolvida pelo Compose para passar banco, usuário, senha e porta à API por variável de ambiente, sem gravar credenciais ou exigir User Secrets. Variáveis do ambiente têm a precedência normal do Compose sobre `.env`.

Os scripts usam `Development`, API na porta 5000 e frontend na 4200. Execute apenas um modo por vez e mantenha essas portas livres. Os logs aparecem no terminal; Ctrl+C encerra backend e frontend. Se um dos processos falhar, o outro também é encerrado. O MySQL continua ativo e o volume é preservado; para pará-lo, use `docker compose stop mysql`. Execute novamente o setup quando as dependências mudarem.

### Execução separada

Backend:

```bash
dotnet run --project backend/Fast.Workshops.Api
```

Frontend, em outro terminal:

```bash
npm --prefix frontend start
```

Acesse [http://localhost:4200](http://localhost:4200). A API roda em [http://localhost:5000/api/atas](http://localhost:5000/api/atas).

Com o backend em `Development` (perfil local padrão), acesse o [Swagger UI](http://localhost:5000/swagger) para consultar e executar os endpoints. O [documento OpenAPI](http://localhost:5000/swagger/v1/swagger.json) é gerado a partir dos controllers. Operações executadas pelo Swagger alteram o mesmo armazenamento usado pelo frontend.

O frontend usa Angular 21, componentes standalone e testes Vitest pelo builder oficial do Angular. A URL da API está em `frontend/src/environments/environment.ts`. A origem CORS está em `backend/Fast.Workshops.Api/appsettings.json` e permite somente `http://localhost:4200` por padrão.

A lista carrega automaticamente seis workshops por vez conforme a rolagem. O botão “Carregar mais encontros” é uma alternativa para teclado ou navegadores sem suporte ao carregamento automático. Cada card mostra o total de participantes e até sete nomes em duas linhas; os detalhes exibem todos.

Filtre por workshop, data e colaborador. Os filtros ficam na URL e são preservados ao voltar. Nos detalhes, Remover exclui apenas a presença, mantendo o cadastro. Cadastros e inclusão de participantes estão disponíveis pela [API](docs/API.md).

## MySQL com Docker Compose

1. Copie `.env.example` para `.env` e substitua as duas senhas.
2. Execute `docker compose up -d --wait`. O serviço usa `mysql:latest`, porta local 3306 (alterável por `MYSQL_PORT`) e volume nomeado `mysql_data` montado em `/var/lib/mysql`.
3. Configure a conexão da API com a mesma senha de `MYSQL_PASSWORD`:

```bash
dotnet user-secrets set "ConnectionStrings:Workshops" "Server=localhost;Port=3306;Database=workshops;User=workshops;Password=SUA_SENHA" --project backend/Fast.Workshops.Api
```

4. Inicie a API com o provider selecionado:

```bash
dotnet run --project backend/Fast.Workshops.Api -- --Persistence:Provider=MySql
```

O Compose lê `.env`; a API usa a configuração nativa do .NET e não lê esse arquivo. User Secrets são carregados no ambiente `Development`. Em outros ambientes, injete `Persistence__Provider=MySql` e `ConnectionStrings__Workshops` no processo. A connection string não deve ser versionada.

O padrão em `appsettings.json` é `InMemory`. Para voltar explicitamente à memória, use `--Persistence:Provider=InMemory`. A troca exige reiniciar a API e não transfere dados entre modos. Configuração desconhecida, conexão ausente/inválida ou banco/schema indisponível impedem a inicialização. Falhas posteriores do MySQL não acionam fallback para memória.

O script `Repositories/MySql/schema.sql` cria as tabelas automaticamente somente quando o volume é inicializado pela primeira vez. Para um banco externo vazio, execute esse SQL no banco `workshops` antes de iniciar a API. Ele é idempotente para criação; mudanças futuras de schema precisam de scripts de migração explícitos.

`docker compose down` preserva os dados. `docker compose down -v` apaga o volume e os dados. Alterar senhas no `.env` não modifica usuários já criados no volume. A tag `latest` acompanha novas versões; faça backup e confira a compatibilidade do volume antes de atualizar a imagem.

Referências: [imagem oficial MySQL](https://hub.docker.com/_/mysql), [provider MySQL para EF Core](https://www.nuget.org/packages/MySql.EntityFrameworkCore/10.0.9).

## Dados de exemplo

### Em memória

No modo `InMemory`, o perfil local ativa `Development` e cria três workshops, quatro colaboradores e três atas com participações variadas. Os dados são reiniciados quando o backend encerra. Fora de `Development`, a memória começa vazia. O modo `MySql` não executa esse seed.

### Semear o MySQL

Com Docker ativo e `.env` configurado, execute:

```powershell
.\scripts\seed-mysql.ps1
```

Ou, em Bash:

```bash
bash ./scripts/seed-mysql.sh
```

O script inicia o MySQL do Compose, aguarda o healthcheck e, em um banco vazio, insere 30 colaboradores, 20 workshops, 20 atas e 480 participações. O calendário fixo cobre 2022 a 2026, com um workshop por trimestre, sempre na segunda quinta-feira de janeiro, abril, julho e outubro, às 16h no offset -03:00. Cada workshop tem 24 participantes, com ausências alternadas de forma determinística pela posição nos exemplos, independentemente dos IDs do banco; cada colaborador participa de 16 encontros. Não exige API ou frontend em execução nem cliente MySQL instalado na máquina.

Pode ser executado novamente, sequencialmente, sem duplicar os exemplos. Reutiliza colaboradores pelo nome e workshops pelo nome e timestamp exato; se houver vários correspondentes, usa o menor ID. Preserva descrições editadas e outros registros; bancos já preenchidos podem exceder as quantidades de exemplo. Participações de exemplo removidas são recriadas ao executar o seed novamente. O seed é explícito e não é executado por `run-mysql` nem pelo startup da API. Os inserts são feitos em uma transação; o script não altera o schema nem apaga dados.

## Validação

Execute toda a validação, sem interação:

```bash
./scripts/check.sh
```

ou

```powershell
.\scripts\check.ps1
```

O script verifica formatação, build, lint e testes dos dois projetos. Consulte [docs/TESTING.md](docs/TESTING.md) para testes focados e regras de isolamento.

No Windows, os scripts `.ps1` usam `npm.cmd`. Para executar os `.sh`, use Git Bash com .NET e Node no PATH; o WSL requer suas próprias instalações de .NET e Node. Encerre os servidores de desenvolvimento antes do setup e da validação para evitar arquivos bloqueados no Windows.

## Contratos

Os endpoints, payloads, validações e respostas de erro estão em [docs/API.md](docs/API.md).

## Métricas de participação

Acesse Métricas no cabeçalho (rota /metricas) para consultar barras de workshops por colaborador e pizza de colaboradores por workshop, com ng2-charts e Chart.js. As contagens vêm de duas requisições ao backend (uma por gráfico), incluem zeros e não dependem dos filtros ou da paginação da listagem. Gráficos e tabelas acessíveis apresentam os totais em ordem decrescente, com nome e ID como desempate; Atualizar métricas busca novamente os totais.

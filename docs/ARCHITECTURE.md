# Arquitetura

## Escopo arquitetural

Use um monorepo com um backend ASP.NET Core MVC e um frontend Angular. O backend é um único projeto; a organização em pastas não representa Clean Architecture nem autoriza projetos separados de domínio, aplicação ou infraestrutura.

Como o backend é uma API, não há Razor Views. O Angular exerce a função de camada de visualização.

## Estrutura

```text
/
├── AGENTS.md
├── README.md
├── compose.yaml
├── .env.example
├── docs/
│   ├── ARCHITECTURE.md
│   ├── API.md
│   └── TESTING.md
├── scripts/
│   ├── setup.sh
│   ├── setup.ps1
│   ├── check.sh
│   ├── check.ps1
│   ├── run-inmemory.sh / run-inmemory.ps1
│   ├── run-mysql.sh / run-mysql.ps1
│   ├── run-project.mjs
│   ├── run-project.test.mjs
│   ├── seed-mysql.sh / seed-mysql.ps1
│   ├── seed-mysql.mjs
│   ├── seed-mysql.sql
│   └── seed-mysql.test.mjs
├── backend/
│   ├── Fast.Workshops.sln
│   ├── Fast.Workshops.Api/
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Contracts/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   │   ├── InMemory/
│   │   │   └── MySql/
│   │   ├── Program.cs
│   │   └── Fast.Workshops.Api.csproj
│   └── tests/
│       ├── Fast.Workshops.Api.Tests/
│       └── Fast.Workshops.MySql.Tests/
└── frontend/
    ├── angular.json
    ├── package.json
    ├── package-lock.json
    └── src/
        ├── app/
        │   ├── core/
        │   ├── features/
        │   │   ├── attendance-records/
        │   │   ├── metrics/
        │   │   └── workshops/
        │   └── shared/
        ├── styles.scss
        └── main.ts
```

Não crie uma pasta até que ela tenha conteúdo real.

## Backend MVC

Fluxo de uma requisição:

```text
HTTP -> Controller -> Service -> Repository interface -> Repository em memória ou MySQL
```

### Models

- `Collaborator`: `Id` positivo e `Name` obrigatório.
- `Workshop`: `Id` positivo, `Name`, `HeldAt` e `Description` obrigatórios.
- `AttendanceRecord`: `Id`, `WorkshopId` e conjunto de `CollaboratorIds`.
- Models mantêm estado e regras simples; não contêm lógica HTTP ou de serialização.
- A ata encapsula `AddCollaborator` e `RemoveCollaborator` e não expõe coleção mutável.

Regras:

- existe no máximo uma ata por workshop;
- workshop e colaborador devem existir antes de serem associados;
- o mesmo colaborador não aparece duas vezes na mesma ata;
- IDs são gerados atomicamente pelos repositories;
- datas usam ISO 8601; filtros comparam a data de calendário.

### Contracts

Request e response DTOs ficam em `Contracts`. Não serialize models diretamente. Use payloads específicos para criação e respostas específicas para consultas.

### Controllers

- `WorkshopsController`;
- `CollaboratorsController`;
- `AttendanceRecordsController`, com rota pública `/api/atas`;
- `MetricsController`, com rota pública `/api/metrics`.

Controllers validam binding, chamam services e transformam resultados em status HTTP. Não acessam armazenamento nem implementam filtros, ordenação ou associação.

### Services

- `WorkshopService`;
- `CollaboratorService`;
- `AttendanceRecordService`;
- `MetricsService`, responsável pelas métricas agregadas.

Services executam cadastros, consultas, filtros, ordenação e mapeamento de DTOs. Permanecem classes concretas. Não crie handlers, commands, queries, use cases ou mediators.

`ResponseProjection` compartilha o mapeamento para DTOs e a ordenação de participantes. `InputRule` centraliza validações de texto, IDs e timestamps. `ApiExceptionFilter` traduz falhas conhecidas em `ProblemDetails`; `BindingProblem` trata solicitações incompatíveis com os contratos.

### Repositories

Defina interfaces pequenas e específicas:

- `IWorkshopRepository`;
- `ICollaboratorRepository`;
- `IAttendanceRecordRepository`.

Implemente-as em `Repositories/InMemory`. Compartilhe o estado por `InMemoryDatabase`, registrado como singleton no container, nunca como singleton estático. Repositories e services são scoped. Garanta thread safety e não devolva coleções internas mutáveis.

O armazenamento usa um lock por instância de `InMemoryDatabase`. IDs, criação exclusiva de atas e alterações de participantes são atômicos. Models de workshop e colaborador são imutáveis; atas retornadas pelos repositories são snapshots independentes.

Não use repositório genérico nem Unit of Work próprio. As mesmas interfaces têm implementações MySQL em `Repositories/MySql`, com Entity Framework Core 10 e o provider oficial `MySql.EntityFrameworkCore`. `WorkshopsDbContext` mapeia o schema existente via Fluent API. `IDbContextFactory<WorkshopsDbContext>` é registrado pelo container; cada operação cria e descarta seu próprio contexto, permitindo chamadas concorrentes sem compartilhar o change tracker. Repositories continuam scoped. Consultas usam LINQ com `AsNoTracking`, gravações usam `SaveChanges` e remoções usam `ExecuteDelete`.

`schema.sql` usa InnoDB, IDs AUTO_INCREMENT, chave única por workshop e chave primária composta para participantes. Foreign keys impedem associações órfãs. Violações de unicidade e referência em `DbUpdateException` são convertidas nas exceções existentes pelo código MySQL interno. A leitura de atas e participantes usa um único SELECT com LEFT JOIN para produzir snapshots coerentes. As entidades internas de persistência ficam junto ao contexto e são convertidas em models, preservando seus IDs positivos, imutabilidade e snapshots. Filtros, ordenação e projeção permanecem nos services.

`held_at` guarda o formato ISO 8601 round-trip em VARCHAR(33), preservando o offset e sete casas decimais; Textos usam utf8mb4 e LONGTEXT, sem introduzir um limite curto nos contratos HTTP.

`PersistenceRegistration` seleciona os três repositories na inicialização por `Persistence:Provider` (`InMemory` por padrão ou `MySql`). Configuração desconhecida e conexão vazia/inválida falham imediatamente. MySQL exige `ConnectionStrings:Workshops` e verifica acesso às tabelas antes de iniciar HTTP. Não há fallback ou migração de dados entre modos; reinicie para trocar. Credenciais ficam em User Secrets no desenvolvimento ou na configuração do ambiente.

O Compose usa `mysql:latest` com volume nomeado em `/var/lib/mysql`, bind da porta somente em loopback e schema inicial em `/docker-entrypoint-initdb.d`. O EF Core não chama `EnsureCreated` nem `Migrate`; não aplica alterações de schema em volumes existentes. O banco externo deve receber o mesmo schema previamente; futuras evoluções exigem migrações explícitas.

### Configuração

- use controllers com roteamento por atributos;
- use o container nativo para injeção por construtor;
- converta erros conhecidos em `ProblemDetails`;
- configure CORS apenas para a origem local do frontend;
- configure logs JSON com o logger do ASP.NET Core;
- disponibilize Swagger UI em `/swagger` e o documento OpenAPI em `/swagger/v1/swagger.json` no ambiente `Development`; os metadados vêm dos controllers via Swashbuckle e incluem descrições, parâmetros, DTOs e respostas HTTP.

### Scripts de desenvolvimento

`run-inmemory` e `run-mysql` têm entradas `.ps1` e `.sh`. Ambas delegam a `scripts/run-project.mjs`, executado pelo Node.js já exigido pelo frontend. O launcher supervisiona API e Angular e encerra os processos que iniciou ao receber interrupção ou falha. No modo MySQL, consulta `docker compose config --format json`, monta a conexão para a porta publicada e aguarda o healthcheck com `up -d --wait`. A conexão é passada somente ao processo da API. O container e o volume permanecem disponíveis após encerrar a aplicação.

### Seed

`DevelopmentSeed` popula os exemplos por meio dos services e é chamado por `Program.cs`. O seed MySQL é independente da API: as entradas `.ps1` e `.sh` delegam a `scripts/seed-mysql.mjs`, que aplica `scripts/seed-mysql.sql` no serviço do Compose. Condições de execução, comandos, conteúdo e regras de reaplicação estão centralizados em [Dados de exemplo](../README.md#dados-de-exemplo).

## Frontend Angular

Use componentes standalone e rotas carregadas por funcionalidade:

```text
app/
├── core/
│   ├── api/
│   ├── models/
│   └── errors/
├── features/
│   ├── attendance-records/
│   ├── metrics/
│   └── workshops/
└── shared/
    ├── empty-state/
    ├── loading-state/
    └── error-state/
```

Rotas:

- `/atas`: listagem e filtros;
- `/workshops/:id`: detalhes;
- `/metricas`: gráficos e tabelas de participação;
- rotas vazia e desconhecida redirecionam para `/atas`.

O frontend consome a API como fonte de verdade; mocks existem somente em testes. Use signals e services Angular simples, sem biblioteca externa de estado. Preserve filtros na query string. A listagem usa `GET /api/atas/pagina` com workshop, data e colaborador filtrados pelo service antes da paginação. A resposta contém `items` e `total`; cada resumo inclui até sete participantes em ordem alfabética e `participantCount` completo. O service mantém filtros, ordenação e paginação sobre os snapshots dos repositories; estes ainda leem todos os registros. A paginação reduz a resposta HTTP e a renderização, mas não a leitura do banco. Datas e nomes empatados usam o ID da ata como desempate estável.

O Angular carrega lotes de seis cards automaticamente por rolagem (`IntersectionObserver`). O botão é uma alternativa para teclado ou ausência dessa API. `exhaustMap` evita buscas simultâneas; trocar filtros cancela a busca com `switchMap` e reinicia a lista. Falhas preservam os cards e permitem repetir a página. A prévia ocupa até duas linhas; os detalhes exibem todos os participantes. O observer é desconectado ao destruir a lista.

`WorkshopsApiService` concentra HTTP. `requestState` compartilha estados de carregamento/erro; `switchMap` cancela buscas anteriores e `takeUntilDestroyed` encerra subscriptions. No Angular, as dependências são resolvidas no construtor com `inject()` e passadas aos métodos que observam as rotas, seguindo o lint oficial. O backend usa parâmetros de construtor. A página de detalhes consulta `GET /api/atas` sem filtros e encontra a ata por `workshop.id`. Workshops sem ata não aparecem nessa consulta; a página informa que a ata não foi encontrada. Cada participante tem a ação Remover, que usa `DELETE /api/atas/{ataId}/colaboradores/{colaboradorId}`. A UI bloqueia cliques repetidos durante a remoção, atualiza os participantes após sucesso e mantém a lista com mensagem de erro em caso de falha.

A UI deve exibir loading, erro e lista vazia; usar labels visíveis; navegar por links reais; funcionar a partir de 320 px; oferecer teclado, foco visível e contraste adequado.

## Métricas agregadas

`MetricsController` expõe os dois endpoints em `/api/metrics`; `MetricsService` agrega snapshots das três interfaces existentes de repositories, com o mesmo comportamento em memória e MySQL. Não há novos projetos ou schema. A agregação ocorre no servidor, mas ainda lê as atas completas, assim como as consultas existentes.

A rota lazy `/metricas`, em `features/metrics`, usa `ng2-charts` e Chart.js para barras horizontais e pizza. `MetricsApiService` consulta `/api/metrics/colaboradores/workshops-count` para todos os totais por colaborador e `/api/metrics/workshops/colaboradores-count` para os totais por workshop: duas requisições por carregamento, sem buscar `/api/colaboradores`. O service percorre uma única leitura das atas para agrupar participações por colaborador, inclui zeros e ordena por nome e ID. A rota individual foi removida. Não há agregação de presenças no navegador. As duas seções possuem estados independentes de loading/erro, retry e tabelas acessíveis com zeros e IDs para diferenciar nomes repetidos. A pizza não é desenhada quando todos os totais são zero. O botão Atualizar recarrega ambas as seções.

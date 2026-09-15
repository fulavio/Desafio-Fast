# Arquitetura

## Escopo arquitetural

Use um monorepo com um backend ASP.NET Core MVC e um frontend Angular. O backend é um único projeto; a organização em pastas não representa Clean Architecture nem autoriza projetos separados de domínio, aplicação ou infraestrutura.

Como o backend é uma API, não há Razor Views. O Angular exerce a função de camada de visualização.

## Estrutura

```text
/
├── AGENTS.md
├── README.md
├── docs/
│   ├── ARCHITECTURE.md
│   ├── API.md
│   └── TESTING.md
├── scripts/
│   ├── setup.sh
│   ├── setup.ps1
│   ├── check.sh
│   └── check.ps1
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
│       └── Fast.Workshops.Api.Tests/
└── frontend/
    ├── angular.json
    ├── package.json
    ├── package-lock.json
    └── src/
        ├── app/
        │   ├── core/
        │   ├── features/
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
- `AttendanceRecordsController`, com rota pública `/api/atas`.

Controllers validam binding, chamam services e transformam resultados em status HTTP. Não acessam armazenamento nem implementam filtros, ordenação ou associação.

### Services

- `WorkshopService`;
- `CollaboratorService`;
- `AttendanceRecordService`.

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

O seed executa uma vez por inicialização somente em `Development` com `InMemory` e inclui pelo menos três workshops, quatro colaboradores, três atas e participações variadas. MySQL começa vazio e preserva os cadastros existentes, sem seed automático. O comando explícito `scripts/seed-mysql.ps1` (ou `.sh`) aplica `scripts/seed-mysql.sql` no serviço local do Compose, em transação. Em banco vazio, inclui 30 colaboradores e 20 workshops trimestrais de 2022 a 2026, na segunda quinta-feira de janeiro, abril, julho e outubro, às 16h (-03:00), com 20 atas e 480 participações. As presenças alternam deterministicamente pela posição nos exemplos, independentemente dos IDs do banco. Reutiliza nomes de colaboradores e nome/timestamp de workshops e completa atas e participações ausentes. Registros e presenças anteriores são preservados, portanto bancos já preenchidos podem exceder essas quantidades. Não altera o seed automático da API. Testes controlam o próprio estado e não dependem de seed manual.

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
│   └── workshops/
└── shared/
    ├── empty-state/
    ├── loading-state/
    └── error-state/
```

Rotas:

- `/atas`: listagem e filtros;
- `/workshops/:id`: detalhes;
- rotas vazia e desconhecida redirecionam para `/atas`.

O frontend consome a API como fonte de verdade; mocks existem somente em testes. Use signals e services Angular simples, sem biblioteca externa de estado. Preserve filtros na query string. Envie workshop e data à API e aplique o filtro de colaborador no cliente.

`WorkshopsApiService` concentra HTTP. `requestState` compartilha estados de carregamento/erro; `switchMap` cancela buscas anteriores e `takeUntilDestroyed` encerra subscriptions. No Angular, as dependências são resolvidas no construtor com `inject()` e passadas aos métodos que observam as rotas, seguindo o lint oficial. O backend usa parâmetros de construtor. A página de detalhes consulta `GET /api/atas` sem filtros e encontra a ata por `workshop.id`. Workshops sem ata não aparecem nessa consulta; a página informa que a ata não foi encontrada. Cada participante tem a ação Remover, que usa `DELETE /api/atas/{ataId}/colaboradores/{colaboradorId}`. A UI bloqueia cliques repetidos durante a remoção, atualiza os participantes após sucesso e mantém a lista com mensagem de erro em caso de falha.

A UI deve exibir loading, erro e lista vazia; usar labels visíveis; navegar por links reais; funcionar a partir de 320 px; oferecer teclado, foco visível e contraste adequado.

## Ordem de implementação

1. Criar solution e projeto MVC.
2. Implementar models e repositories em memória.
3. Implementar contracts, services e controllers.
4. Adicionar tratamento de erros, seed e testes do backend.
5. Criar cliente HTTP, listagem, filtros e detalhes no Angular.
6. Adicionar estados, acessibilidade, responsividade e testes do frontend.
7. Executar `./scripts/check.sh` e atualizar a documentação.

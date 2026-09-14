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
│   │   │   └── InMemory/
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
HTTP -> Controller -> Service -> Repository interface -> Repository em memória
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

Não use repositório genérico nem Unit of Work próprio. As interfaces existem porque a persistência será substituída futuramente; não implemente EF Core ou banco nesta entrega.

### Configuração

- use controllers com roteamento por atributos;
- use o container nativo para injeção por construtor;
- converta erros conhecidos em `ProblemDetails`;
- configure CORS apenas para a origem local do frontend;
- configure logs JSON com o logger do ASP.NET Core;
- não habilite Swagger/OpenAPI.

### Seed

O seed de desenvolvimento executa uma vez e inclui pelo menos três workshops, quatro colaboradores, três atas e participações variadas. Testes controlam o próprio estado e não dependem desse seed.

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

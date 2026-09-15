# Testes e validação

## Regras

- Toda função nova recebe teste.
- Toda correção recebe teste de regressão.
- Testes seguem F.I.R.S.T.: rápidos, independentes, repetíveis, autoverificáveis e oportunos.
- I/O externo é substituído por classes fake nomeadas, nunca stubs inline.
- Testes em memória não dependem de rede, credenciais, relógio real, ordem, seed manual ou estado de outro teste. Integração MySQL usa containers locais descartáveis preparados automaticamente; não usa banco externo nem o volume do Compose.
- Toda execução é headless, não interativa e produz saída previsível.
- Não reduza assertions nem desabilite testes para obter sucesso.

## Comando completo

Após executar `./scripts/setup.sh`, valide o repositório com:

Mantenha Docker com containers Linux em execução. A primeira execução precisa baixar `mysql:latest` e a imagem do resource reaper do Testcontainers; depois pode usar o cache local. O setup restaura os pacotes, mas não inicia banco nem altera volumes.

```bash
./scripts/check.sh
```

ou

```powershell
.\scripts\setup.ps1
.\scripts\check.ps1
```

## Backend

Os scripts executam, em sequência: testes do launcher com `node --test scripts/run-project.test.mjs`, `dotnet format --verify-no-changes`, build e testes .NET, Prettier, ESLint, build de produção Angular e testes Angular sem watch. A primeira falha interrompe a execução com código diferente de zero.

No Windows, use PowerShell ou Git Bash com as ferramentas Windows no PATH. WSL é um ambiente separado e precisa de .NET/Node próprios. Feche os servidores antes de executar o setup ou os checks, pois executáveis podem estar bloqueados no Windows.

```bash
dotnet test backend/Fast.Workshops.sln --no-restore
```

Cubra:

- criação válida e rejeição de campos vazios;
- criação de ata e rejeição de ata duplicada;
- adição idempotente e remoção de colaborador;
- IDs e associações inexistentes;
- ordenação de colaboradores;
- projeção de workshops por colaborador;
- filtros de ata por nome, data e combinação;
- status e JSON de cada endpoint.

Use o servidor de testes do ASP.NET Core para integração. Cada teste cria e controla seu próprio estado.

Implementação: xUnit e `WebApplicationFactory<Program>` em ambiente `Testing`, sem seed automático. Os testes de regras usam repositories reais em memória; os de concorrência exercitam IDs atômicos, ata única, associação idempotente e snapshots. Datas são constantes e nenhuma chamada usa rede externa.

`WorkshopApiFactory` força `InMemory`, independentemente das variáveis locais. `PersistenceConfigurationTests` verifica seleção consistente dos três repositories e rejeição de configuração inválida sem revelar credenciais.

O projeto `Fast.Workshops.MySql.Tests` faz parte da solução e da validação completa. Testcontainers cria bancos isolados por classe, portas disponíveis e senha descartável, aplica o mesmo `schema.sql` do Compose e remove os containers ao terminar. Os casos controlam seus próprios registros e não dependem da ordem. Cobrem repositories reais com EF Core e contextos independentes por operação, SQL gerado com parâmetros, precisão/fuso de datas, IDs concorrentes, ata única, associação idempotente, integridade referencial, snapshots, remoção, contratos HTTP, filtros, ausência de seed e persistência após reiniciar a API. Falhas de servidor e schema impedem startup. Docker ausente faz a suíte falhar; não há testes ignorados silenciosamente.

Testes focados:

```bash
dotnet test backend/tests/Fast.Workshops.Api.Tests --no-restore
dotnet test backend/tests/Fast.Workshops.MySql.Tests --no-restore
```

`SwaggerDocumentationTests` usa um host em `Development` para verificar a página Swagger UI, a geração do OpenAPI, os sete endpoints, respostas de erro `ProblemDetails` e descrições dos filtros. Esses testes inspecionam a documentação e não dependem dos dados do seed.

## Frontend

```bash
npm --prefix frontend test -- --watch=false
```

Cubra:

- carregamento inicial;
- loading, erro e lista vazia;
- filtro por colaborador;
- filtros remotos por workshop e data;
- limpeza dos filtros;
- query string;
- navegação para detalhes;
- renderização do workshop e participantes.

Use as ferramentas oficiais do Angular. Simule somente a fronteira HTTP com classes fake nomeadas.

Implementação: `ng test` com o builder oficial `@angular/build:unit-test`, Vitest e jsdom. `FakeWorkshopServer` encapsula `HttpTestingController` e verifica parâmetros; componentes e roteador são reais (`RouterTestingHarness`). A suíte cobre retry, cancelamento de buscas, preservação dos filtros, links sem parâmetros vazios, estados e foco do atalho de conteúdo. Os detalhes são testados usando exclusivamente GET /api/atas, incluindo ausência da ata e seleção pelo ID do workshop. Os testes de remoção verificam o ID da ata, bloqueio de duplicidade, sucesso, último participante e falha com nova tentativa. Não exige Chrome instalado.

## Verificações manuais

Quando houver mudança visual, confira os fluxos essenciais em desktop e em viewport de 320 px. Verifique teclado, foco, contraste, labels, loading, erro e ausência de resultados.

## Cobertura

Não há meta numérica de cobertura nesta entrega. Priorize regras, contratos e regressões relevantes.

## Scripts de execução

Os testes do launcher usam processos e comandos fake nomeados, sem iniciar Docker ou servidores. Cobrem configuração MySQL e escape de senha, isolamento do modo em memória, falhas e encerramento dos processos ao receber Ctrl+C. Execute-os isoladamente com `node --test scripts/run-project.test.mjs`.

`seed-mysql.test.mjs` verifica a chamada ao Compose e a propagação de falhas usando comandos fake, sem Docker. `MySqlSeedTests` executa o SQL real em container descartável e verifica quantidades, repetição sem duplicatas, IDs estáveis, Unicode, timestamps e preservação de registros e descrições editados. Ambas as suítes fazem parte de `check.sh` e `check.ps1`.

# AGENTS.md

## Missão

Implemente o desafio FullStack de participação em workshops da FAST Soluções. Use um único backend ASP.NET Core MVC e um frontend Angular. Leia este arquivo antes de alterar código.

## Documentação obrigatória

- [README.md](README.md): visão geral, setup e execução.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): escopo, MVC, estrutura e responsabilidades.
- [docs/API.md](docs/API.md): contratos HTTP, validação e erros.
- [docs/TESTING.md](docs/TESTING.md): estratégia e comandos de teste.

Se uma mudança afetar comportamento, arquitetura, comandos ou contratos, atualize o documento correspondente no mesmo commit.

## Escopo

- Implemente somente os requisitos obrigatórios do desafio.
- Não implemente banco de dados, autenticação, autorização ou gráficos.
- Mantenha os dados em memória nesta entrega.
- Preserve a possibilidade de trocar repositories em memória por persistência real posteriormente.
- Não aplique Clean Architecture, arquitetura hexagonal ou onion architecture.
- Não crie projetos `Domain`, `Application` ou `Infrastructure`.
- Não adicione CQRS, MediatR, Unit of Work próprio, repositório genérico, mensageria, cache distribuído ou microserviços.

## Arquitetura

- Backend: um projeto ASP.NET Core MVC com `Models`, `Contracts`, `Controllers`, `Services` e `Repositories`.
- Frontend: Angular com `core`, `features` e `shared`; não crie pastas vazias.
- Controllers validam HTTP, chamam services e produzem respostas.
- Services concentram regras, filtros, ordenação e mapeamento de DTOs.
- Services dependem de `IWorkshopRepository`, `ICollaboratorRepository` e `IAttendanceRecordRepository`.
- Implemente os repositories atuais em `Repositories/InMemory`.
- Use o container nativo para injeção por construtor. Nunca use singleton estático.
- O Angular é a camada de visualização; não use Razor Views.

## Clean Code

- Funções: 4–20 linhas. Arquivos: menos de 500 linhas, idealmente 200–300.
- Uma responsabilidade por função e módulo.
- Use nomes específicos e pesquisáveis. Evite `data`, `handler` e `Manager`.
- Prefira nomes que retornem menos de cinco ocorrências em uma busca textual no código.
- Tipos de parâmetros e retorno são explícitos. Não use `object`, `dynamic` ou `any` soltos.
- Elimine duplicação; extraia lógica compartilhada.
- Prefira early return. Máximo de dois níveis de indentação.
- Inclua o valor recebido e o formato esperado em mensagens de erro.
- Preserve comentários válidos. Atualize os desatualizados; remova apenas comentários óbvios.
- Comentários explicam o porquê e registram issue/commit quando houver restrição externa.
- Funções públicas recebem XML documentation ou TSDoc com intenção e exemplo.
- Injete dependências por construtor/parâmetro.
- Isole dependências externas substituíveis atrás de interfaces finas do projeto; não envolva APIs básicas do framework sem necessidade.
- Use `dotnet format` e Prettier; não crie regras paralelas de estilo.
- Produza logs de aplicação em JSON estruturado. Nunca registre dados sensíveis.

## Testes e comandos

- Toda função nova recebe teste; todo bug recebe teste de regressão.
- Use classes fake nomeadas para I/O; não use stubs inline.
- Testes seguem F.I.R.S.T. e não dependem de rede, relógio real, ordem, credenciais ou preparação manual.
- Execute setup idempotente com `./scripts/setup.sh`.
- Execute toda a validação headless com `./scripts/check.sh`.
- Não remova testes, reduza assertions ou desabilite regras para obter sucesso.

## Fluxo de trabalho

1. Leia a documentação relacionada à tarefa.
2. Inspecione o código existente com `rg` e preserve alterações alheias.
3. Faça a menor mudança coesa dentro do escopo.
4. Atualize testes e documentação junto com o código.
5. Execute testes focados durante o desenvolvimento.
6. Execute `./scripts/check.sh` antes de concluir.
7. Verifique manualmente fluxos de UI em desktop e mobile quando houver mudança visual.
8. Relate arquivos alterados, verificações executadas e limitações restantes.

Não declare sucesso enquanto uma verificação obrigatória falhar.

# Testes e validação

## Regras

- Toda função nova recebe teste.
- Toda correção recebe teste de regressão.
- Testes seguem F.I.R.S.T.: rápidos, independentes, repetíveis, autoverificáveis e oportunos.
- I/O externo é substituído por classes fake nomeadas, nunca stubs inline.
- Testes não dependem de rede, credenciais, relógio real, ordem, seed manual ou estado de outro teste.
- Toda execução é headless, não interativa e produz saída previsível.
- Não reduza assertions nem desabilite testes para obter sucesso.

## Comando completo

Após executar `./scripts/setup.sh`, valide o repositório com:

```bash
./scripts/check.sh
```

## Backend

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

## Verificações manuais

Quando houver mudança visual, confira os fluxos essenciais em desktop e em viewport de 320 px. Verifique teclado, foco, contraste, labels, loading, erro e ausência de resultados.

## Cobertura

Não há meta numérica de cobertura nesta entrega. Priorize regras, contratos e regressões relevantes ao desafio.

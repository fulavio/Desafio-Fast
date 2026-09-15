# Contrato da API

Todos os endpoints usam JSON. Erros seguem `ProblemDetails` (`application/problem+json`) com `status`, `title` e `detail`. Arrays vazios nunca são serializados como `null`.

Os contratos são os mesmos em `InMemory` e `MySql`. O armazenamento é selecionado ao iniciar a API, sem migração nem fallback automático. O modo MySQL preserva dados entre reinícios e não insere exemplos de desenvolvimento. Falha de conexão/schema impede a inicialização; indisponibilidade durante uma requisição produz erro inesperado `500` em `ProblemDetails`, sem detalhes internos.

No ambiente `Development`, a documentação interativa fica em [Swagger UI](http://localhost:5000/swagger) e o contrato gerado em [OpenAPI JSON](http://localhost:5000/swagger/v1/swagger.json). Os controllers declaram descrições, parâmetros e respostas com seus DTOs e códigos HTTP. O botão **Try it out** permite executar as operações na API local.

## `POST /api/workshops`

```json
{
  "name": "Clean Code",
  "heldAt": "2026-10-08T16:00:00-03:00",
  "description": "Práticas para código legível e sustentável."
}
```

Retorna `201 Created` com o workshop criado. Retorna `400 Bad Request` para dados inválidos.

## `POST /api/colaboradores`

```json
{
  "name": "Ana Souza"
}
```

Retorna `201 Created` com o colaborador criado. Retorna `400 Bad Request` para nome inválido.

## `POST /api/atas`

```json
{
  "workshopId": 1
}
```

Retorna `201 Created` com lista inicial de colaboradores vazia. Retorna `404 Not Found` se o workshop não existir e `409 Conflict` se ele já possuir ata.

## `PUT /api/atas/{ataId}/colaboradores/{colaboradorId}`

Não recebe corpo. Retorna `204 No Content` ao criar a associação ou quando ela já existir. Retorna `404 Not Found` se a ata ou o colaborador não existir.

## `DELETE /api/atas/{ataId}/colaboradores/{colaboradorId}`

Não recebe corpo. Retorna `204 No Content` ao remover. Retorna `404 Not Found` se a ata, o colaborador ou a associação não existir.

## `GET /api/colaboradores`

Retorna colaboradores em ordem alfabética com os workshops de que participaram:

```json
[
  {
    "id": 1,
    "name": "Ana Souza",
    "workshops": [
      {
        "id": 1,
        "name": "Clean Code",
        "heldAt": "2026-10-08T16:00:00-03:00",
        "description": "Práticas para código legível e sustentável."
      }
    ]
  }
]
```

## `GET /api/atas`

Parâmetros opcionais:

- `workshopNome`: correspondência parcial, sem diferenciar maiúsculas e minúsculas, após trim;
- `data`: data exata em `yyyy-MM-dd`.

Parâmetros combinados usam `AND`. Sem parâmetros, retorna todas as atas. Ordene por data decrescente e, em empate, por nome do workshop. Ordene os colaboradores de cada ata alfabeticamente.

```json
[
  {
    "id": 1,
    "workshop": {
      "id": 1,
      "name": "Clean Code",
      "heldAt": "2026-10-08T16:00:00-03:00",
      "description": "Práticas para código legível e sustentável."
    },
    "collaborators": [
      {
        "id": 1,
        "name": "Ana Souza"
      }
    ]
  }
]
```

Data inválida retorna `400 Bad Request`. Ausência de resultados retorna `200 OK` com `[]`.

A página de detalhes consulta este endpoint sem filtros e localiza a ata por `workshop.id`. Sem uma ata correspondente, exibe “Ata não encontrada”. A remoção de presença usa o `id` da ata no endpoint `DELETE`, preservando o cadastro do colaborador.

## `GET /api/atas/pagina`

Consulta paginada usada pela listagem de workshops; `GET /api/atas` continua retornando atas completas para a tela de detalhes.

- `workshopNome` e `data`: mesmos filtros de `GET /api/atas`.
- `colaborador`: parte do nome de qualquer participante, sem diferenciar maiúsculas e minúsculas, após trim. Pesquisa todos os participantes, incluindo os que não aparecem na prévia.
- `pagina`: inteiro a partir de 1; padrão 1.
- `tamanhoPagina`: inteiro de 1 a 50; padrão 6.

Os filtros usam `AND` e são aplicados antes da paginação. Ordena por data decrescente, nome do workshop e ID da ata como desempate. Resposta `200 OK`:

```json
{
  "items": [
    {
      "id": 1,
      "workshop": {
        "id": 1,
        "name": "Clean Code",
        "heldAt": "2026-07-09T16:00:00-03:00",
        "description": "Práticas para código legível."
      },
      "collaborators": [{ "id": 1, "name": "Ana Souza" }],
      "participantCount": 1
    }
  ],
  "total": 1
}
```

`collaborators` inclui no máximo os sete primeiros nomes em ordem alfabética; `participantCount` inclui todas as presenças. `total` é o número de atas após os filtros, antes da paginação. Uma página além do fim retorna `items: []` e mantém `total`; nenhum resultado retorna `items: []` e `total: 0`. Parâmetros inválidos retornam `400` com `ProblemDetails`. Cada página consulta o estado atual; alterações concorrentes podem mudar os limites entre páginas.

## Validação

- `name`: obrigatório e não pode conter apenas espaços;
- `description`: obrigatória e não pode conter apenas espaços;
- `heldAt`: ISO 8601 com horário, segundos e fuso (`Z` ou `±HH:mm`); aceita até sete casas de fração de segundo;
- IDs: inteiros positivos;
- `data`: formato exato `yyyy-MM-dd`;
- remova espaços externos antes de salvar ou filtrar;
- não imponha unicidade aos nomes.

Mensagens de erro incluem o valor problemático e o formato esperado, sem stack traces ou detalhes internos. Erros esperados nunca retornam `500`.

O filtro `data` considera a data de calendário no fuso armazenado, sem convertê-la para UTC. `data=` vazio é inválido; para não filtrar por data, omita o parâmetro. A ordenação considera o instante do workshop e usa nome como desempate, sem diferenciar maiúsculas de minúsculas.

Erros de binding retornam `400` com o campo e o formato esperado. Quando o JSON não pode ser convertido, a mensagem identifica `JSON inválido ou incompatível` sem repetir o corpo da requisição. O middleware padrão fornece `ProblemDetails` para falhas inesperadas, sem expor stack traces.

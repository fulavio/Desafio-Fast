# Contrato da API

Todos os endpoints usam JSON. Erros seguem `ProblemDetails` (`application/problem+json`) com `status`, `title` e `detail`. Arrays vazios nunca são serializados como `null`.

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

## Validação

- `name`: obrigatório e não pode conter apenas espaços;
- `description`: obrigatória e não pode conter apenas espaços;
- `heldAt`: ISO 8601 válido;
- IDs: inteiros positivos;
- `data`: formato exato `yyyy-MM-dd`;
- remova espaços externos antes de salvar ou filtrar;
- não imponha unicidade aos nomes.

Mensagens de erro incluem o valor problemático e o formato esperado, sem stack traces ou detalhes internos. Erros esperados nunca retornam `500`.

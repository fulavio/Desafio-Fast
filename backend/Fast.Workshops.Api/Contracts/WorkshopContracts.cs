using Swashbuckle.AspNetCore.Annotations;

namespace Fast.Workshops.Api.Contracts;

[SwaggerSchema(Required = new[] { "name", "heldAt", "description" })]
public sealed record CreateWorkshopRequest(
    [property: SwaggerSchema(Description = "Nome obrigatório, não pode conter apenas espaços.")] string? Name,
    [property: SwaggerSchema(Description = "Data e horário ISO 8601 com fuso; exemplo: 2026-10-08T16:00:00-03:00.", Format = "date-time")] string? HeldAt,
    [property: SwaggerSchema(Description = "Descrição obrigatória, não pode conter apenas espaços.")] string? Description);
public sealed record WorkshopResponse(int Id, string Name, DateTimeOffset HeldAt, string Description);

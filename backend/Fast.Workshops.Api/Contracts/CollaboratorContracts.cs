using Swashbuckle.AspNetCore.Annotations;

namespace Fast.Workshops.Api.Contracts;

[SwaggerSchema(Required = new[] { "name" })]
public sealed record CreateCollaboratorRequest(
    [property: SwaggerSchema(Description = "Nome obrigatório; nomes repetidos são permitidos.")] string? Name);
public sealed record CollaboratorResponse(int Id, string Name);
public sealed record CollaboratorParticipationResponse(int Id, string Name, IReadOnlyList<WorkshopResponse> Workshops);

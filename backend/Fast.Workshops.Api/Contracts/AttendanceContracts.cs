using Swashbuckle.AspNetCore.Annotations;

namespace Fast.Workshops.Api.Contracts;

[SwaggerSchema(Required = new[] { "workshopId" })]
public sealed record CreateAttendanceRequest(
    [property: SwaggerSchema(Description = "ID inteiro positivo de um workshop existente e sem ata; exemplo: 1.")] int WorkshopId);
public sealed record AttendanceResponse(int Id, WorkshopResponse Workshop, IReadOnlyList<CollaboratorResponse> Collaborators);

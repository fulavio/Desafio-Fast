namespace Fast.Workshops.Api.Contracts;

public sealed record CreateCollaboratorRequest(string? Name);
public sealed record CollaboratorResponse(int Id, string Name);
public sealed record CollaboratorParticipationResponse(int Id, string Name, IReadOnlyList<WorkshopResponse> Workshops);

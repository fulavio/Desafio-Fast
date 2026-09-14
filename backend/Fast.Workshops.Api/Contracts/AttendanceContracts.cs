namespace Fast.Workshops.Api.Contracts;

public sealed record CreateAttendanceRequest(int WorkshopId);
public sealed record AttendanceResponse(int Id, WorkshopResponse Workshop, IReadOnlyList<CollaboratorResponse> Collaborators);

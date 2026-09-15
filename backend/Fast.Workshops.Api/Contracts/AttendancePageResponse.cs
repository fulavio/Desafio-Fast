namespace Fast.Workshops.Api.Contracts;

public sealed record AttendancePageResponse(IReadOnlyList<AttendanceSummaryResponse> Items, int Total);
public sealed record AttendanceSummaryResponse(int Id, WorkshopResponse Workshop,
    IReadOnlyList<CollaboratorResponse> Collaborators, int ParticipantCount);

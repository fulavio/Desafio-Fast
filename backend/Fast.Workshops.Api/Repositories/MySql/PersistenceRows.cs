namespace Fast.Workshops.Api.Repositories.MySql;

// Persistence state stays separate so generated IDs and EF navigation collections do not weaken model invariants.
internal sealed class WorkshopRow
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset HeldAt { get; set; }
    public required string Description { get; set; }
}

internal sealed class CollaboratorRow
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

internal sealed class AttendanceRow
{
    public int Id { get; set; }
    public int WorkshopId { get; set; }
    public List<ParticipantRow> Participants { get; set; } = [];
}

internal sealed class ParticipantRow
{
    public int AttendanceId { get; set; }
    public int CollaboratorId { get; set; }
}

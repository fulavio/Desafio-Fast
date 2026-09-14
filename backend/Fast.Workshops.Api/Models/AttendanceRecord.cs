namespace Fast.Workshops.Api.Models;

public sealed class AttendanceRecord
{
    private readonly HashSet<int> collaboratorIds = [];
    public int Id { get; }
    public int WorkshopId { get; }
    public IReadOnlyList<int> CollaboratorIds => collaboratorIds.Order().ToArray();

    /// <summary>Creates an empty attendance record; e.g. record 1 for workshop 2.</summary>
    public AttendanceRecord(int id, int workshopId)
    {
        Id = InputRule.PositiveId(id);
        WorkshopId = InputRule.PositiveId(workshopId);
    }

    /// <summary>Adds an ID once; e.g. adding collaborator 1 twice leaves one participant.</summary>
    public bool AddCollaborator(int collaboratorId) =>
        collaboratorIds.Add(InputRule.PositiveId(collaboratorId));

    /// <summary>Removes an ID and reports whether it existed; e.g. participant 1.</summary>
    public bool RemoveCollaborator(int collaboratorId) =>
        collaboratorIds.Remove(InputRule.PositiveId(collaboratorId));

    /// <summary>Returns an independent snapshot; e.g. callers cannot mutate stored attendance.</summary>
    public AttendanceRecord Snapshot()
    {
        var snapshot = new AttendanceRecord(Id, WorkshopId);
        foreach (var collaboratorId in collaboratorIds)
            snapshot.AddCollaborator(collaboratorId);
        return snapshot;
    }
}

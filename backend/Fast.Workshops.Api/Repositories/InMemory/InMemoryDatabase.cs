using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Repositories.InMemory;

public sealed class InMemoryDatabase
{
    internal object SyncRoot { get; } = new();
    internal Dictionary<int, Workshop> Workshops { get; } = [];
    internal Dictionary<int, Collaborator> Collaborators { get; } = [];
    internal Dictionary<int, AttendanceRecord> AttendanceRecords { get; } = [];
    internal int WorkshopSequence;
    internal int CollaboratorSequence;
    internal int AttendanceSequence;
}

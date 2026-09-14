using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Repositories.InMemory;

public sealed class InMemoryWorkshopRepository(InMemoryDatabase database) : IWorkshopRepository
{
    /// <inheritdoc />
    public Workshop Create(string name, DateTimeOffset heldAt, string description)
    {
        lock (database.SyncRoot)
        {
            var workshop = new Workshop(++database.WorkshopSequence, name, heldAt, description);
            database.Workshops.Add(workshop.Id, workshop);
            return workshop;
        }
    }

    /// <inheritdoc />
    public Workshop? Find(int id)
    {
        lock (database.SyncRoot)
            return database.Workshops.GetValueOrDefault(id);
    }

    /// <inheritdoc />
    public IReadOnlyList<Workshop> List()
    {
        lock (database.SyncRoot)
            return database.Workshops.Values.ToArray();
    }
}

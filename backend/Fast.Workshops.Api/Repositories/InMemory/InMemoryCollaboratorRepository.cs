using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Repositories.InMemory;

public sealed class InMemoryCollaboratorRepository(InMemoryDatabase database) : ICollaboratorRepository
{
    /// <inheritdoc />
    public Collaborator Create(string name)
    {
        lock (database.SyncRoot)
        {
            var collaborator = new Collaborator(++database.CollaboratorSequence, name);
            database.Collaborators.Add(collaborator.Id, collaborator);
            return collaborator;
        }
    }

    /// <inheritdoc />
    public Collaborator? Find(int id)
    {
        lock (database.SyncRoot)
            return database.Collaborators.GetValueOrDefault(id);
    }

    /// <inheritdoc />
    public IReadOnlyList<Collaborator> List()
    {
        lock (database.SyncRoot)
            return database.Collaborators.Values.ToArray();
    }
}

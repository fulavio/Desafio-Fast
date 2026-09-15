using Fast.Workshops.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Fast.Workshops.Api.Repositories.MySql;

public sealed class MySqlCollaboratorRepository(IDbContextFactory<WorkshopsDbContext> contexts) : ICollaboratorRepository
{
    /// <inheritdoc />
    public Collaborator Create(string name)
    {
        using var context = contexts.CreateDbContext();
        var collaborator = new CollaboratorRow { Name = name };
        context.Add(collaborator);
        context.SaveChanges();
        return new(collaborator.Id, collaborator.Name);
    }

    /// <inheritdoc />
    public Collaborator? Find(int id) => ReadCollaborators(id).SingleOrDefault();

    /// <inheritdoc />
    public IReadOnlyList<Collaborator> List() => ReadCollaborators(null);

    private IReadOnlyList<Collaborator> ReadCollaborators(int? id)
    {
        using var context = contexts.CreateDbContext();
        var collaborators = context.Set<CollaboratorRow>().AsNoTracking();
        if (id.HasValue) collaborators = collaborators.Where(row => row.Id == id.Value);
        return collaborators.AsEnumerable().Select(row => new Collaborator(row.Id, row.Name)).ToArray();
    }
}

using Fast.Workshops.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Fast.Workshops.Api.Repositories.MySql;

public sealed class MySqlWorkshopRepository(IDbContextFactory<WorkshopsDbContext> contexts) : IWorkshopRepository
{
    /// <inheritdoc />
    public Workshop Create(string name, DateTimeOffset heldAt, string description)
    {
        using var context = contexts.CreateDbContext();
        var workshop = new WorkshopRow { Name = name, HeldAt = heldAt, Description = description };
        context.Add(workshop);
        context.SaveChanges();
        return new(workshop.Id, workshop.Name, workshop.HeldAt, workshop.Description);
    }

    /// <inheritdoc />
    public Workshop? Find(int id) => ReadWorkshops(id).SingleOrDefault();

    /// <inheritdoc />
    public IReadOnlyList<Workshop> List() => ReadWorkshops(null);

    private IReadOnlyList<Workshop> ReadWorkshops(int? id)
    {
        using var context = contexts.CreateDbContext();
        var workshops = context.Set<WorkshopRow>().AsNoTracking();
        if (id.HasValue) workshops = workshops.Where(row => row.Id == id.Value);
        return workshops.AsEnumerable().Select(row => new Workshop(row.Id, row.Name, row.HeldAt, row.Description)).ToArray();
    }
}

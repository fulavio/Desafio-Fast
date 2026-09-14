using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Repositories;

public interface IWorkshopRepository
{
    /// <summary>Saves a workshop with a generated ID; e.g. a new Clean Code session.</summary>
    Workshop Create(string name, DateTimeOffset heldAt, string description);
    /// <summary>Returns a workshop or null; e.g. Find(1).</summary>
    Workshop? Find(int id);
    /// <summary>Returns a snapshot of all workshops; e.g. for attendance projections.</summary>
    IReadOnlyList<Workshop> List();
}

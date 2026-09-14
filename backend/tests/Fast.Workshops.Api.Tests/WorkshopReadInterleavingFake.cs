using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories;

namespace Fast.Workshops.Api.Tests;

public sealed class WorkshopReadInterleavingFake(IWorkshopRepository workshops,
    IAttendanceRecordRepository records) : IWorkshopRepository
{
    /// <inheritdoc />
    public Workshop Create(string name, DateTimeOffset heldAt, string description) =>
        workshops.Create(name, heldAt, description);

    /// <inheritdoc />
    public Workshop? Find(int id) => workshops.Find(id);

    /// <summary>Publishes an attendance after taking the workshop snapshot, simulating a concurrent writer.</summary>
    public IReadOnlyList<Workshop> List()
    {
        var snapshot = workshops.List();
        var published = workshops.Create("Published during read", DateTimeOffset.MinValue, "Description");
        records.Create(published.Id);
        return snapshot;
    }
}

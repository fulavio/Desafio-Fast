using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Services;

namespace Fast.Workshops.Api.Tests;

public sealed class SnapshotConsistencyTests
{
    /// <summary>Does not project attendance newer than its resource snapshots; e.g. a write during List.</summary>
    [Fact]
    public void AttendanceSnapshotCannotReferenceAnUnreadWorkshop()
    {
        var database = new InMemoryDatabase();
        var workshops = new InMemoryWorkshopRepository(database);
        var records = new InMemoryAttendanceRecordRepository(database);
        var people = new InMemoryCollaboratorRepository(database);
        var interleaving = new WorkshopReadInterleavingFake(workshops, records);
        var existing = interleaving.Create("Existing", DateTimeOffset.MinValue, "Description");
        records.Create(existing.Id);
        var service = new AttendanceRecordService(records, interleaving, people);
        Assert.Equal(existing.Id, Assert.Single(service.List(null, null)).Workshop.Id);
        Assert.Equal(2, records.List().Count);
        Assert.Equal(existing, interleaving.Find(existing.Id));
    }
}

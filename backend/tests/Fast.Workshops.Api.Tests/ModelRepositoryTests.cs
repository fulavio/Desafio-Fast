using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories.InMemory;

namespace Fast.Workshops.Api.Tests;

public sealed class ModelRepositoryTests
{
    /// <summary>Protects model invariants and detached snapshots; e.g. external lists cannot change attendance.</summary>
    [Fact]
    public void AttendanceModelIsIdempotentAndDetached()
    {
        var attendance = new AttendanceRecord(1, 1);
        Assert.True(attendance.AddCollaborator(2));
        Assert.False(attendance.AddCollaborator(2));
        var snapshot = attendance.Snapshot();
        Assert.True(snapshot.RemoveCollaborator(2));
        Assert.Single(attendance.CollaboratorIds);
        Assert.False(snapshot.RemoveCollaborator(2));
        Assert.Throws<InputException>(() => attendance.AddCollaborator(0));
        Assert.Throws<InputException>(() => attendance.RemoveCollaborator(-1));
        Assert.Throws<InputException>(() => new AttendanceRecord(0, 1));
        Assert.Throws<InputException>(() => new AttendanceRecord(1, 0));
        Assert.Throws<InputException>(() => new Collaborator(0, "Ana"));
        Assert.Throws<InputException>(() => new Collaborator(1, " "));
        Assert.Throws<InputException>(() => new Workshop(1, "", DateTimeOffset.MinValue, "d"));
        Assert.Throws<InputException>(() => new Workshop(1, "Code", DateTimeOffset.MinValue, ""));
    }

    /// <summary>Rejects malformed timestamps; e.g. locale dates and missing offsets.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("08/10/2026")]
    [InlineData("2026-10-08")]
    [InlineData("2026-10-08T16:00:00")]
    [InlineData("2026-02-30T16:00:00Z")]
    public void TimestampRequiresIsoOffset(string? received) =>
        Assert.Throws<InputException>(() => InputRule.Timestamp(received));

    /// <summary>Accepts ISO fractions and offsets; e.g. a UTC millisecond timestamp.</summary>
    [Theory]
    [InlineData("2026-10-08T16:00:00Z")]
    [InlineData("2026-10-08T16:00:00.123Z")]
    [InlineData("2026-10-08T16:00:00.123-03:00")]
    public void TimestampAcceptsIsoValues(string received) =>
        Assert.Equal(8, InputRule.Timestamp(received).Day);

    /// <summary>Checks atomic IDs and duplicate protection under contention; e.g. 40 simultaneous creates.</summary>
    [Fact]
    public void RepositoryOperationsAreAtomicAndSnapshotsAreIndependent()
    {
        var database = new InMemoryDatabase();
        var workshops = new InMemoryWorkshopRepository(database);
        var people = new InMemoryCollaboratorRepository(database);
        var records = new InMemoryAttendanceRecordRepository(database);
        Parallel.For(0, 40, index => workshops.Create("Code", DateTimeOffset.MinValue, "d"));
        Parallel.For(0, 40, index => people.Create("Ana"));
        Assert.Equal(40, workshops.List().Select(workshop => workshop.Id).Distinct().Count());
        Assert.Equal(40, people.List().Select(person => person.Id).Distinct().Count());
        Parallel.For(0, 40, index => TryCreateAttendance(records));
        Assert.Single(records.List());
        Parallel.For(0, 40, index => records.AddCollaborator(1, 1));
        records.Find(1)!.RemoveCollaborator(1);
        records.List()[0].RemoveCollaborator(1);
        Assert.Single(records.Find(1)!.CollaboratorIds);
        Assert.Null(workshops.Find(99));
        Assert.Null(people.Find(99));
        Assert.Null(records.Find(99));
        Assert.Throws<MissingResourceException>(() => records.Create(99));
        Assert.Throws<MissingResourceException>(() => records.AddCollaborator(99, 1));
        Assert.Throws<MissingResourceException>(() => records.AddCollaborator(1, 99));
    }

    private static void TryCreateAttendance(InMemoryAttendanceRecordRepository records)
    {
        try { records.Create(1); }
        catch (DuplicateAttendanceException) { /* Competing calls must reject the duplicate. */ }
    }
}

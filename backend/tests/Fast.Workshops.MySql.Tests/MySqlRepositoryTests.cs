using System.Globalization;
using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories.MySql;
using MySqlConnector;

namespace Fast.Workshops.MySql.Tests;

public sealed class MySqlRepositoryTests(MySqlTestDatabase database) : IClassFixture<MySqlTestDatabase>
{
    /// <summary>Persists exact text, duplicate names and offset timestamps across connections.</summary>
    [Fact]
    public void RoundTripsWorkshopsAndCollaborators()
    {
        using var connections = new MySqlDataSource(database.ConnectionString);
        var workshops = new MySqlWorkshopRepository(connections);
        var collaborators = new MySqlCollaboratorRepository(connections);
        var instant = DateTimeOffset.Parse("2026-10-08T23:59:59.1234567-03:00", CultureInfo.InvariantCulture);
        var workshop = workshops.Create("SQL ' ; 🧪", instant, new string('x', 1000));
        var collaborator = collaborators.Create("Ana ' 🧪");
        var duplicateName = collaborators.Create(collaborator.Name);
        Assert.NotEqual(collaborator.Id, duplicateName.Id);
        AssertWorkshopRoundTrip(workshops, workshop);
        Assert.Equal(collaborator.Name, collaborators.Find(collaborator.Id)!.Name);
        Assert.Contains(collaborators.List(), entry => entry.Id == duplicateName.Id && entry.Name == duplicateName.Name);
        Assert.Null(workshops.Find(int.MaxValue));
        Assert.Null(collaborators.Find(int.MaxValue));
    }

    private static void AssertWorkshopRoundTrip(MySqlWorkshopRepository workshops, Workshop workshop)
    {
        var restored = workshops.Find(workshop.Id)!;
        Assert.Equal((workshop.Id, workshop.Name, workshop.Description, workshop.HeldAt),
            (restored.Id, restored.Name, restored.Description, restored.HeldAt));
        Assert.Equal(workshop.HeldAt.Offset, restored.HeldAt.Offset);
        Assert.Contains(workshops.List(), entry => entry.Id == workshop.Id && entry.Name == workshop.Name);
    }

    /// <summary>Enforces foreign keys, detached snapshots and participant removal.</summary>
    [Fact]
    public void EnforcesAttendanceRelationships()
    {
        using var connections = new MySqlDataSource(database.ConnectionString);
        var records = new MySqlAttendanceRecordRepository(connections);
        var workshop = new MySqlWorkshopRepository(connections).Create("Relationships", DateTimeOffset.UnixEpoch, "Test");
        var collaborator = new MySqlCollaboratorRepository(connections).Create("Participant");
        var attendance = records.Create(workshop.Id);
        Assert.Empty(records.Find(attendance.Id)!.CollaboratorIds);
        Assert.Throws<MissingResourceException>(() => records.Create(int.MaxValue));
        Assert.Throws<DuplicateAttendanceException>(() => records.Create(workshop.Id));
        Assert.Throws<MissingResourceException>(() => records.AddCollaborator(attendance.Id, int.MaxValue));
        Assert.Throws<MissingResourceException>(() => records.AddCollaborator(int.MaxValue, collaborator.Id));
        records.AddCollaborator(attendance.Id, collaborator.Id);
        records.Find(attendance.Id)!.RemoveCollaborator(collaborator.Id);
        Assert.Equal(new[] { collaborator.Id }, records.List().Single(record => record.Id == attendance.Id).CollaboratorIds);
        records.RemoveCollaborator(attendance.Id, collaborator.Id);
        Assert.Throws<MissingResourceException>(() => records.RemoveCollaborator(attendance.Id, collaborator.Id));
        Assert.Empty(records.Find(attendance.Id)!.CollaboratorIds);
        Assert.Null(records.Find(int.MaxValue));
    }

    /// <summary>Database constraints make concurrent creates and participant additions atomic.</summary>
    [Fact]
    public async Task SerializesConcurrentWrites()
    {
        using var connections = new MySqlDataSource(database.ConnectionString);
        var records = new MySqlAttendanceRecordRepository(connections);
        var workshops = new MySqlWorkshopRepository(connections);
        var created = await Task.WhenAll(Enumerable.Range(0, 12).Select(index => Task.Run(() =>
            workshops.Create($"Concurrent {index}", DateTimeOffset.UnixEpoch, "Test"))));
        Assert.Equal(12, created.Select(workshop => workshop.Id).Distinct().Count());
        var attempts = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() => TryCreate(records, created[0].Id))));
        Assert.Single(attempts, success => success);
        var attendance = records.List().Single(record => record.WorkshopId == created[0].Id);
        var collaborator = new MySqlCollaboratorRepository(connections).Create("Concurrent participant");
        await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() => records.AddCollaborator(attendance.Id, collaborator.Id))));
        Assert.Equal(new[] { collaborator.Id }, records.Find(attendance.Id)!.CollaboratorIds);
    }

    private static bool TryCreate(MySqlAttendanceRecordRepository records, int workshopId)
    {
        try { records.Create(workshopId); return true; }
        catch (DuplicateAttendanceException) { return false; }
    }
}

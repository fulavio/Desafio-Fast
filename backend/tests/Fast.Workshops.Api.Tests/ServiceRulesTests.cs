using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Services;

namespace Fast.Workshops.Api.Tests;

public sealed class ServiceRulesTests
{
    private readonly InMemoryDatabase database = new();

    /// <summary>Combines filters using local calendar dates; e.g. 23:30 -03 remains October 8.</summary>
    [Fact]
    public void FiltersOrderAndProjectionUseStoredCalendarDate()
    {
        var workshops = new InMemoryWorkshopRepository(database);
        var people = new InMemoryCollaboratorRepository(database);
        var records = new InMemoryAttendanceRecordRepository(database);
        var service = new AttendanceRecordService(records, workshops, people);
        var older = workshops.Create("Older", InputRule.Timestamp("2026-01-01T10:00:00Z"), "d");
        var zebra = workshops.Create("Zebra Code", InputRule.Timestamp("2026-10-08T23:30:00-03:00"), "d");
        var alpha = workshops.Create("Alpha Code", zebra.HeldAt, "d");
        foreach (var workshop in new[] { older, zebra, alpha }) records.Create(workshop.Id);
        var bruno = people.Create("Bruno");
        var ana = people.Create("Ana");
        records.AddCollaborator(2, bruno.Id);
        records.AddCollaborator(2, ana.Id);
        Assert.Equal(new[] { alpha.Id, zebra.Id, older.Id }, service.List(null, null).Select(record => record.Workshop.Id));
        Assert.Equal(2, service.List(" CODE ", "2026-10-08").Count);
        Assert.Empty(service.List("code", "2026-10-09"));
        Assert.Equal(new[] { "Ana", "Bruno" }, service.List("zebra", null).Single().Collaborators.Select(person => person.Name));
        Assert.Equal(new[] { "Ana", "Bruno" }, new CollaboratorService(people, workshops, records).List().Select(person => person.Name));
    }

    /// <summary>Validates service association boundaries; e.g. missing person returns a known error.</summary>
    [Fact]
    public void AssociationRulesRejectMissingResources()
    {
        var workshops = new InMemoryWorkshopRepository(database);
        var people = new InMemoryCollaboratorRepository(database);
        var records = new InMemoryAttendanceRecordRepository(database);
        var service = new AttendanceRecordService(records, workshops, people);
        var workshop = workshops.Create("Code", InputRule.Timestamp("2026-10-08T16:00:00Z"), "d");
        var attendance = service.Create(new(workshop.Id));
        Assert.Throws<MissingResourceException>(() => service.AddCollaborator(attendance.Id, 99));
        Assert.Throws<MissingResourceException>(() => service.RemoveCollaborator(attendance.Id, 99));
        Assert.Throws<InputException>(() => service.AddCollaborator(0, 1));
        Assert.Throws<DuplicateAttendanceException>(() => service.Create(new(workshop.Id)));
        var person = people.Create("Ana");
        Assert.Throws<MissingResourceException>(() => service.RemoveCollaborator(attendance.Id, person.Id));
    }

    /// <summary>Checks deterministic development seed; e.g. three sessions and four people.</summary>
    [Fact]
    public void DevelopmentSeedProvidesVariedParticipation()
    {
        var workshops = new InMemoryWorkshopRepository(database);
        var people = new InMemoryCollaboratorRepository(database);
        var records = new InMemoryAttendanceRecordRepository(database);
        var sessionService = new WorkshopService(workshops);
        var peopleService = new CollaboratorService(people, workshops, records);
        var attendanceService = new AttendanceRecordService(records, workshops, people);
        new DevelopmentSeed(sessionService, peopleService, attendanceService).Populate();
        Assert.Equal(3, workshops.List().Count);
        Assert.Equal(4, people.List().Count);
        Assert.Equal(new[] { 3, 3, 2 }, attendanceService.List(null, null).Select(record => record.Collaborators.Count));
        Assert.Equal(3, peopleService.List().Single(person => person.Name == "Bruno Lima").Workshops.Count);
    }
}

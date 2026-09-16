using System.Net;
using System.Net.Http.Json;
using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Services;


namespace Fast.Workshops.Api.Tests;

public sealed class MetricsTests
{
    /// <summary>Counts distinct presences, preserves zeros and updates after removal.</summary>
    [Fact]
    public void CountsPresencesAndIncludesWorkshopsWithoutRecords()
    {
        var database = new InMemoryDatabase();
        var people = new InMemoryCollaboratorRepository(database);
        var workshops = new InMemoryWorkshopRepository(database);
        var records = new InMemoryAttendanceRecordRepository(database);
        var metrics = new MetricsService(people, workshops, records);
        Assert.Empty(metrics.CountCollaborators());
        Assert.Empty(metrics.CountWorkshops());
        var bruno = people.Create("Bruno");
        var ana = people.Create("Ana");
        var other = people.Create("Ana");
        var first = workshops.Create("Code", InputRule.Timestamp("2026-01-01T12:00:00Z"), "Code");
        var second = workshops.Create("Angular", first.HeldAt, "Angular");
        var empty = workshops.Create("Angular", first.HeldAt, "Empty");
        var attendance = records.Create(first.Id);
        records.AddCollaborator(attendance.Id, ana.Id);
        records.AddCollaborator(attendance.Id, ana.Id);
        records.AddCollaborator(attendance.Id, bruno.Id);
        records.AddCollaborator(records.Create(second.Id).Id, ana.Id);
        Assert.Equal(new CollaboratorWorkshopsCountResponse(ana.Id, "Ana", 2), metrics.CountWorkshops().Single(item => item.CollaboratorId == ana.Id));
        Assert.Equal(0, metrics.CountWorkshops().Single(item => item.CollaboratorId == other.Id).WorkshopsCount);
        Assert.Equal(new[] { ana.Id, bruno.Id, other.Id }, metrics.CountWorkshops().Select(item => item.CollaboratorId));
        Assert.Equal(new[] { 2, 1, 0 }, metrics.CountWorkshops().Select(item => item.WorkshopsCount));
        Assert.Equal(new[] { first.Id, second.Id, empty.Id }, metrics.CountCollaborators().Select(item => item.WorkshopId));
        Assert.Equal(new[] { 2, 1, 0 }, metrics.CountCollaborators().Select(item => item.CollaboratorsCount));
        records.RemoveCollaborator(attendance.Id, ana.Id);
        Assert.Equal(1, metrics.CountWorkshops().Single(item => item.CollaboratorId == ana.Id).WorkshopsCount);
        Assert.Equal(1, metrics.CountCollaborators().Single(item => item.WorkshopId == first.Id).CollaboratorsCount);
        Assert.Equal(new[] { second.Id, first.Id, empty.Id }, metrics.CountCollaborators().Select(item => item.WorkshopId));
    }

    /// <summary>Breaks equal totals by case-insensitive name and then ID for both charts.</summary>
    [Fact]
    public void EqualTotalsUseNameAndIdAsTieBreakers()
    {
        var database = new InMemoryDatabase();
        var people = new InMemoryCollaboratorRepository(database);
        var workshops = new InMemoryWorkshopRepository(database);
        var metrics = new MetricsService(people, workshops, new InMemoryAttendanceRecordRepository(database));
        foreach (var name in new[] { "Zeta", "ana", "Ana" })
        {
            people.Create(name);
            workshops.Create(name, InputRule.Timestamp("2026-01-01T12:00:00Z"), name);
        }
        Assert.Equal(new[] { 2, 3, 1 }, metrics.CountWorkshops().Select(item => item.CollaboratorId));
        Assert.Equal(new[] { 2, 3, 1 }, metrics.CountCollaborators().Select(item => item.WorkshopId));
    }

    /// <summary>Exposes typed aggregate JSON using the development examples.</summary>
    [Fact]
    public async Task EndpointsReturnAggregateContracts()
    {
        using var factory = new WorkshopApiFactory("Development");
        using var client = factory.CreateClient();
        var people = (await client.GetFromJsonAsync<CollaboratorParticipationResponse[]>("/api/colaboradores"))!;
        var counts = (await client.GetFromJsonAsync<CollaboratorWorkshopsCountResponse[]>("/api/metrics/colaboradores/workshops-count"))!;
        Assert.Equal(people.Length, counts.Length);
        foreach (var person in people)
        {
            var metric = counts.Single(item => item.CollaboratorId == person.Id);
            Assert.Equal(new(person.Id, person.Name, person.Workshops.Count), metric);
        }
        var metrics = (await client.GetFromJsonAsync<WorkshopCollaboratorsCountResponse[]>("/api/metrics/workshops/colaboradores-count"))!;
        var records = (await client.GetFromJsonAsync<AttendanceResponse[]>("/api/atas"))!;
        Assert.Equal(records.Length, metrics.Length);
        Assert.All(metrics, metric => Assert.Equal(records.Single(record => record.Workshop.Id == metric.WorkshopId).Collaborators.Count, metric.CollaboratorsCount));
    }

    /// <summary>Returns empty arrays and removes the former individual route.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("2147483648")]
    [InlineData("99")]
    public async Task EmptyMetricsAndRemovedIndividualRoute(string id)
    {
        using var factory = new WorkshopApiFactory();
        using var client = factory.CreateClient();
        Assert.Empty((await client.GetFromJsonAsync<CollaboratorWorkshopsCountResponse[]>("/api/metrics/colaboradores/workshops-count"))!);
        Assert.Empty((await client.GetFromJsonAsync<WorkshopCollaboratorsCountResponse[]>("/api/metrics/workshops/colaboradores-count"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/metrics/colaboradores/{id}/workshops-count")).StatusCode);
        var created = await client.PostAsJsonAsync("/api/colaboradores", new CreateCollaboratorRequest("Sem presença"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var person = (await created.Content.ReadFromJsonAsync<CollaboratorResponse>())!;
        var counts = (await client.GetFromJsonAsync<CollaboratorWorkshopsCountResponse[]>("/api/metrics/colaboradores/workshops-count"))!;
        Assert.Equal(new CollaboratorWorkshopsCountResponse(person.Id, person.Name, 0), Assert.Single(counts));
    }
}

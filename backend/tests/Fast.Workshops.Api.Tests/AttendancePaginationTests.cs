using System.Net;
using System.Net.Http.Json;
using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fast.Workshops.Api.Tests;

public sealed class AttendancePaginationTests
{
    private readonly AttendanceRecordService service;

    public AttendancePaginationTests()
    {
        var database = new InMemoryDatabase();
        var workshops = new InMemoryWorkshopRepository(database);
        var people = new InMemoryCollaboratorRepository(database);
        var records = new InMemoryAttendanceRecordRepository(database);
        service = new(records, workshops, people);
        for (var index = 1; index <= 8; index++) people.Create($"Pessoa {index:00}");
        for (var index = 1; index <= 14; index++)
        {
            var workshop = workshops.Create("Workshop", InputRule.Timestamp("2026-07-09T23:30:00-03:00"), "Descrição");
            var record = records.Create(workshop.Id);
            for (var person = 1; person <= (index == 14 ? 8 : 7); person++) records.AddCollaborator(record.Id, person);
        }
    }

    /// <summary>Pages tied dates and names deterministically; e.g. 14 records become 6, 6, 2.</summary>
    [Fact]
    public void PagesHaveStableOrderAndCompleteCoverage()
    {
        var pages = Enumerable.Range(1, 3).Select(page => service.ListPage(null, null, null, page, 6)).ToArray();
        Assert.All(pages, page => Assert.Equal(14, page.Total));
        Assert.Equal(new[] { 6, 6, 2 }, pages.Select(page => page.Items.Count));
        Assert.Equal(Enumerable.Range(1, 14), pages.SelectMany(page => page.Items).Select(record => record.Id));
        Assert.Empty(service.ListPage(null, null, null, 4, 6).Items);
        Assert.Empty(service.ListPage(null, null, null, int.MaxValue, 50).Items);
    }

    /// <summary>Matches participants outside the preview before pagination; e.g. Pessoa 08 on the last record.</summary>
    [Fact]
    public void FiltersUseAllParticipantsAndSummariesKeepFullCounts()
    {
        var page = service.ListPage(" WORK ", "2026-07-09", " pessoa 08 ", 1, 6);
        Assert.Equal(1, page.Total);
        var summary = Assert.Single(page.Items);
        Assert.Equal(14, summary.Id);
        Assert.Equal(8, summary.ParticipantCount);
        Assert.Equal(Enumerable.Range(1, 7).Select(index => $"Pessoa {index:00}"), summary.Collaborators.Select(person => person.Name));
        Assert.Equal(8, service.List(null, null).Single(record => record.Id == 14).Collaborators.Count);
        Assert.Equal(0, service.ListPage(null, "2026-07-10", null, 1, 6).Total);
        Assert.Empty(service.ListPage(null, null, "Nobody", 1, 6).Items);
    }

    /// <summary>Rejects invalid page bounds; e.g. tamanhoPagina=51 is outside 1–50.</summary>
    [Theory]
    [InlineData(0, 6)]
    [InlineData(-1, 6)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public void InvalidPageBoundsIncludeReceivedValues(int page, int pageSize)
    {
        var error = Assert.Throws<InputException>(() => service.ListPage(null, null, null, page, pageSize));
        Assert.Contains($"'{page}'", error.Message);
        Assert.Contains($"'{pageSize}'", error.Message);
    }

    /// <summary>Binds paginated filters over HTTP; e.g. Bruno participates in all development examples.</summary>
    [Fact]
    public async Task PageEndpointBindsFiltersAndReturnsTotals()
    {
        using var factory = new WorkshopApiFactory("Development");
        using var client = factory.CreateClient();
        var page = (await client.GetFromJsonAsync<AttendancePageResponse>("/api/atas/pagina?pagina=2&tamanhoPagina=2&colaborador=Bruno"))!;
        Assert.Equal(3, page.Total);
        Assert.Equal("APIs com ASP.NET Core", Assert.Single(page.Items).Workshop.Name);
        var filtered = (await client.GetFromJsonAsync<AttendancePageResponse>("/api/atas/pagina?workshopNome=code&data=2026-07-09&colaborador=Ana"))!;
        Assert.Equal(1, filtered.Total);
        Assert.Equal("Clean Code", Assert.Single(filtered.Items).Workshop.Name);
    }

    /// <summary>Returns ProblemDetails for malformed pages and dates; e.g. pagina=zero.</summary>
    [Theory]
    [InlineData("pagina=0")]
    [InlineData("pagina=zero")]
    [InlineData("tamanhoPagina=51")]
    [InlineData("data=")]
    [InlineData("data=2026-02-30")]
    public async Task InvalidPageQueriesReturnBadRequest(string query)
    {
        using var factory = new WorkshopApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/atas/pagina?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal(400, problem.Status);
        Assert.Contains("esperado", problem.Detail);
    }
}

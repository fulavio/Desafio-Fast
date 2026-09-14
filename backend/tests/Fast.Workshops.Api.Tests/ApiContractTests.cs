using System.Net;
using System.Net.Http.Json;
using System.Text;
using Fast.Workshops.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fast.Workshops.Api.Tests;

public sealed class ApiContractTests
{
    /// <summary>Exercises the full lifecycle; e.g. duplicate adds remain idempotent.</summary>
    [Fact]
    public async Task AttendanceLifecyclePreservesContracts()
    {
        using var factory = new WorkshopApiFactory();
        using var client = factory.CreateClient();
        var workshop = await CreateWorkshop(client);
        var person = await CreatePerson(client, " Ana ");
        var created = await client.PostAsJsonAsync("/api/atas", new { workshopId = workshop.Id });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var attendance = (await created.Content.ReadFromJsonAsync<AttendanceResponse>())!;
        Assert.Empty(attendance.Collaborators);
        await AssertStatus(client.PostAsJsonAsync("/api/atas", new { workshopId = workshop.Id }), HttpStatusCode.Conflict);
        var association = $"/api/atas/{attendance.Id}/colaboradores/{person.Id}";
        await AssertStatus(client.PutAsync(association, null), HttpStatusCode.NoContent);
        await AssertStatus(client.PutAsync(association, null), HttpStatusCode.NoContent);
        await AssertParticipant(client, workshop.Id, person.Id);
        await AssertStatus(client.DeleteAsync(association), HttpStatusCode.NoContent);
        await AssertStatus(client.DeleteAsync(association), HttpStatusCode.NotFound);
    }

    /// <summary>Rejects invalid fields with safe ProblemDetails; e.g. a whitespace name.</summary>
    [Theory]
    [InlineData("/api/workshops", """{"name":" ","heldAt":"2026-10-08T16:00:00Z","description":"ok"}""", "name")]
    [InlineData("/api/workshops", """{"name":"ok","heldAt":"yesterday","description":"ok"}""", "yesterday")]
    [InlineData("/api/workshops", """{"name":"ok","heldAt":"2026-10-08T16:00:00Z","description":""}""", "description")]
    [InlineData("/api/workshops", """{"name":"ok","description":"ok"}""", "null")]
    [InlineData("/api/colaboradores", """{"name":"  "}""", "name")]
    [InlineData("/api/atas", """{"workshopId":0}""", "0")]
    [InlineData("/api/atas", """{"workshopId":"invalid"}""", "JSON")]
    [InlineData("/api/atas", "{", "JSON")]
    public async Task InvalidRequestsReturnProblemDetails(string path, string payload, string expected)
    {
        using var factory = new WorkshopApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsync(path, new StringContent(payload, Encoding.UTF8, "application/json"));
        await AssertProblem(response, HttpStatusCode.BadRequest, expected);
    }

    /// <summary>Distinguishes malformed IDs, dates and missing resources; e.g. workshop 99 is 404.</summary>
    [Theory]
    [InlineData("/api/atas?data=2026-02-30", HttpStatusCode.BadRequest)]
    [InlineData("/api/atas?data=08-10-2026", HttpStatusCode.BadRequest)]
    [InlineData("/api/atas?data=", HttpStatusCode.BadRequest)]



    public async Task InvalidQueriesReturnExpectedStatus(string path, HttpStatusCode expected)
    {
        using var factory = new WorkshopApiFactory();
        using var client = factory.CreateClient();
        await AssertProblem(await client.GetAsync(path), expected, "esperado");
    }

    /// <summary>Isolates host state and permits workshops without attendance; e.g. a new host is empty.</summary>
    [Fact]
    public async Task EmptyHostAndUnrecordedWorkshopReturnEmptyArrays()
    {
        using var factory = new WorkshopApiFactory();
        using var client = factory.CreateClient();
        Assert.Empty((await client.GetFromJsonAsync<AttendanceResponse[]>("/api/atas"))!);
        Assert.Empty((await client.GetFromJsonAsync<CollaboratorParticipationResponse[]>("/api/colaboradores"))!);
        var workshop = await CreateWorkshop(client);
        Assert.Empty((await client.GetFromJsonAsync<AttendanceResponse[]>("/api/atas"))!);
        var created = await client.PostAsJsonAsync("/api/atas", new { workshopId = workshop.Id });
        var detail = (await created.Content.ReadFromJsonAsync<AttendanceResponse>())!;
        Assert.Equal("Clean Code", detail!.Workshop.Name);
        Assert.Empty(detail.Collaborators);
        await AssertStatus(client.PostAsJsonAsync("/api/atas", new { workshopId = 99 }), HttpStatusCode.NotFound);
        await AssertStatus(client.PutAsync("/api/atas/99/colaboradores/99", null), HttpStatusCode.NotFound);
        await AssertStatus(client.DeleteAsync("/api/atas/-1/colaboradores/1"), HttpStatusCode.BadRequest);
    }

    /// <summary>Allows only the configured development origin; e.g. localhost:4200.</summary>
    [Theory]
    [InlineData("http://localhost:4200", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task CorsRestrictsOrigins(string origin, bool allowed)
    {
        using var factory = new WorkshopApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/atas");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static async Task<WorkshopResponse> CreateWorkshop(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/workshops",
            new { name = " Clean Code ", heldAt = "2026-10-08T16:00:00-03:00", description = " Práticas " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var workshop = (await response.Content.ReadFromJsonAsync<WorkshopResponse>())!;
        Assert.Equal("Práticas", workshop.Description);
        return workshop;
    }

    private static async Task<CollaboratorResponse> CreatePerson(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/colaboradores", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var collaborator = (await response.Content.ReadFromJsonAsync<CollaboratorResponse>())!;
        Assert.Equal(name.Trim(), collaborator.Name);
        return collaborator;
    }

    private static async Task AssertParticipant(HttpClient client, int workshopId, int collaboratorId)
    {
        var records = (await client.GetFromJsonAsync<AttendanceResponse[]>("/api/atas"))!;
        var detail = Assert.Single(records, record => record.Workshop.Id == workshopId);
        Assert.Equal(collaboratorId, Assert.Single(detail.Collaborators).Id);
        var people = (await client.GetFromJsonAsync<CollaboratorParticipationResponse[]>("/api/colaboradores"))!;
        Assert.Equal(workshopId, Assert.Single(Assert.Single(people).Workshops).Id);
    }

    private static async Task AssertStatus(Task<HttpResponseMessage> request, HttpStatusCode expected)
    {
        using var response = await request;
        Assert.Equal(expected, response.StatusCode);
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode expected, string text)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal((int)expected, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.Contains(text, problem.Detail);
        Assert.DoesNotContain("StackTrace", await response.Content.ReadAsStringAsync());
    }
}

using System.Net;
using System.Net.Http.Json;
using Fast.Workshops.Api.Contracts;
using MySqlConnector;

namespace Fast.Workshops.MySql.Tests;

public sealed class MySqlHttpTests(MySqlTestDatabase database) : IClassFixture<MySqlTestDatabase>
{
    /// <summary>Preserves HTTP contracts and data across API restarts without Development seed.</summary>
    [Fact]
    public async Task KeepsDataAcrossApiRestarts()
    {
        AttendanceResponse attendance;
        CollaboratorResponse collaborator;
        using (var factory = new MySqlApiFactory(database.ConnectionString))
        using (var client = factory.CreateClient())
        {
            Assert.Empty((await client.GetFromJsonAsync<AttendanceResponse[]>("/api/atas"))!);
            var workshop = await Create<WorkshopResponse, CreateWorkshopRequest>(client, "/api/workshops", new("Offset SQL", "2026-10-08T23:59:59.1234567-03:00", "Persistent"));
            collaborator = await Create<CollaboratorResponse, CreateCollaboratorRequest>(client, "/api/colaboradores", new("Ana"));
            attendance = await Create<AttendanceResponse, CreateAttendanceRequest>(client, "/api/atas", new(workshop.Id));
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/atas", new CreateAttendanceRequest(workshop.Id))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync($"/api/atas/{attendance.Id}/colaboradores/{collaborator.Id}", null)).StatusCode);
        }
        using var restarted = new MySqlApiFactory(database.ConnectionString);
        using var restartedClient = restarted.CreateClient();
        await AssertPersistedAttendance(restartedClient, attendance, collaborator);
    }

    private static async Task AssertPersistedAttendance(HttpClient client, AttendanceResponse attendance, CollaboratorResponse collaborator)
    {
        var filtered = await client.GetFromJsonAsync<AttendanceResponse[]>("/api/atas?workshopNome=offset&data=2026-10-08");
        Assert.Equal(attendance.Id, Assert.Single(filtered!).Id);
        Assert.Equal(collaborator, Assert.Single(filtered![0].Collaborators));
        Assert.Empty((await client.GetFromJsonAsync<AttendanceResponse[]>("/api/atas?data=2026-10-09"))!);
        var people = await client.GetFromJsonAsync<CollaboratorParticipationResponse[]>("/api/colaboradores");
        Assert.Equal(attendance.Workshop.Id, Assert.Single(Assert.Single(people!).Workshops).Id);
        var route = $"/api/atas/{attendance.Id}/colaboradores/{collaborator.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/atas?data=invalid")).StatusCode);
    }

    /// <summary>A missing schema prevents startup instead of silently selecting memory.</summary>
    [Fact]
    public void RejectsMissingSchema()
    {
        var settings = new MySqlConnectionStringBuilder(database.ConnectionString) { Database = "information_schema" };
        using var factory = new MySqlApiFactory(settings.ConnectionString);
        Assert.Throws<MySqlException>(() => factory.CreateClient());
    }

    /// <summary>An unreachable server prevents startup without opening an in-memory store.</summary>
    [Fact]
    public void RejectsUnavailableServer()
    {
        var settings = new MySqlConnectionStringBuilder(database.ConnectionString)
        { Port = 1, ConnectionTimeout = 1, Pooling = false };
        using var factory = new MySqlApiFactory(settings.ConnectionString);
        Assert.Throws<MySqlException>(() => factory.CreateClient());
    }

    private static async Task<TResponse> Create<TResponse, TRequest>(HttpClient client, string route, TRequest request)
    {
        using var response = await client.PostAsJsonAsync(route, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}

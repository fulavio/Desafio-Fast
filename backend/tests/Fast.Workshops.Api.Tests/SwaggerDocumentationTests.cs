using System.Text.Json;

namespace Fast.Workshops.Api.Tests;

public sealed class SwaggerDocumentationTests
{
    /// <summary>Documents the actual response contracts; e.g. POST attendance reports 201, 400, 404 and 409.</summary>
    [Theory]
    [InlineData("/api/workshops", "post", "201,400")]
    [InlineData("/api/colaboradores", "post", "201,400")]
    [InlineData("/api/colaboradores", "get", "200")]
    [InlineData("/api/atas", "post", "201,400,404,409")]
    [InlineData("/api/atas", "get", "200,400")]
    [InlineData("/api/atas/pagina", "get", "200,400")]
    [InlineData("/api/atas/{ataId}/colaboradores/{colaboradorId}", "put", "204,400,404")]
    [InlineData("/api/atas/{ataId}/colaboradores/{colaboradorId}", "delete", "204,400,404")]
    public async Task OpenApiDescribesEndpointResponses(string path, string method, string statuses)
    {
        using var factory = new WorkshopApiFactory("Development");
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        foreach (var status in statuses.Split(','))
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
        if (statuses.Contains("400"))
            AssertProblemSchema(operation.GetProperty("responses").GetProperty("400"));
    }

    /// <summary>Exposes the UI and explains query formats; e.g. data requires yyyy-MM-dd.</summary>
    [Fact]
    public async Task SwaggerUiAndFilterDocumentationAreAvailable()
    {
        using var factory = new WorkshopApiFactory("Development");
        using var client = factory.CreateClient();
        Assert.Contains("swagger-ui", await client.GetStringAsync("/swagger/index.html"));
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var parameters = document.RootElement.GetProperty("paths").GetProperty("/api/atas")
            .GetProperty("get").GetProperty("parameters").EnumerateArray().ToArray();
        Assert.Equal(new[] { "workshopNome", "data" }, parameters.Select(parameter => parameter.GetProperty("name").GetString()));
        Assert.Contains("yyyy-MM-dd", parameters[1].GetProperty("description").GetString());
        Assert.All(parameters, parameter => Assert.False(parameter.TryGetProperty("required", out var required) && required.GetBoolean()));
    }

    private static void AssertProblemSchema(JsonElement response)
    {
        var schema = response.GetProperty("content").GetProperty("application/problem+json").GetProperty("schema");
        Assert.Equal("#/components/schemas/ProblemDetails", schema.GetProperty("$ref").GetString());
    }

    /// <summary>Documents required body fields and ISO timestamps; e.g. heldAt has date-time format.</summary>
    [Fact]
    public async Task RequestSchemasDocumentRequiredFields()
    {
        using var factory = new WorkshopApiFactory("Development");
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var workshop = schemas.GetProperty("CreateWorkshopRequest");
        Assert.Equal(new[] { "description", "heldAt", "name" }, workshop.GetProperty("required").EnumerateArray().Select(field => field.GetString()).Order());
        Assert.Equal("date-time", workshop.GetProperty("properties").GetProperty("heldAt").GetProperty("format").GetString());
        Assert.Equal("name", schemas.GetProperty("CreateCollaboratorRequest").GetProperty("required")[0].GetString());
        Assert.Equal("workshopId", schemas.GetProperty("CreateAttendanceRequest").GetProperty("required")[0].GetString());
    }
}

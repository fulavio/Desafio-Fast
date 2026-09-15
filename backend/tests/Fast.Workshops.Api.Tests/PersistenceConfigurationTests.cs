using Fast.Workshops.Api.Repositories;
using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Repositories.MySql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.Workshops.Api.Tests;

public sealed class PersistenceConfigurationTests
{
    /// <summary>Resolves all repositories consistently; e.g. MySql never registers memory state.</summary>
    [Theory]
    [InlineData(null, false)]
    [InlineData("InMemory", false)]
    [InlineData("MySql", true)]
    public void SelectsRepositoriesAtStartup(string? provider, bool usesMySql)
    {
        var services = new ServiceCollection();
        Assert.Equal(usesMySql, services.AddPersistence(Settings(provider, "Server=localhost;Database=workshops")));
        using var container = services.BuildServiceProvider();
        using var scope = container.CreateScope();
        Assert.IsType(usesMySql ? typeof(MySqlWorkshopRepository) : typeof(InMemoryWorkshopRepository), scope.ServiceProvider.GetRequiredService<IWorkshopRepository>());
        Assert.IsType(usesMySql ? typeof(MySqlCollaboratorRepository) : typeof(InMemoryCollaboratorRepository), scope.ServiceProvider.GetRequiredService<ICollaboratorRepository>());
        Assert.IsType(usesMySql ? typeof(MySqlAttendanceRecordRepository) : typeof(InMemoryAttendanceRecordRepository), scope.ServiceProvider.GetRequiredService<IAttendanceRecordRepository>());
        Assert.Equal(!usesMySql, container.GetService<InMemoryDatabase>() is not null);
    }

    /// <summary>Rejects unknown providers and malformed settings without exposing passwords.</summary>
    [Theory]
    [InlineData("mysql", null, "mysql")]
    [InlineData("", null, "InMemory ou MySql")]
    [InlineData("MySql", null, "vazia")]
    [InlineData("MySql", " ", "vazia")]
    [InlineData("MySql", "Password=secret-value", "inválida")]
    [InlineData("MySql", "Unknown=secret-value", "inválida")]
    [InlineData("MySql", "Database=workshops;Port=secret-value", "inválida")]
    [InlineData("MySql", "Database=workshops;Port=999999999999999999999", "inválida")]
    public void RejectsInvalidConfiguration(string provider, string? connection, string expected)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddPersistence(Settings(provider, connection)));
        Assert.Contains(expected, exception.Message);
        Assert.DoesNotContain("secret-value", exception.Message);
    }

    private static IConfiguration Settings(string? provider, string? connection) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = provider,
            ["ConnectionStrings:Workshops"] = connection
        }).Build();
}

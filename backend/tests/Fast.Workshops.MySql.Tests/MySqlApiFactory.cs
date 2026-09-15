using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fast.Workshops.MySql.Tests;

public sealed class MySqlApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
        .UseEnvironment("Development")
        .UseSetting("Persistence:Provider", "MySql")
        .UseSetting("ConnectionStrings:Workshops", connectionString);
}

using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Repositories.MySql;
using MySqlConnector;

namespace Fast.Workshops.Api.Repositories;

public static class PersistenceRegistration
{
    /// <summary>Selects repositories at startup; e.g. Persistence:Provider=MySql.</summary>
    public static bool AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"] ?? "InMemory";
        if (provider == "InMemory")
        {
            services.AddSingleton<InMemoryDatabase>();
            services.AddScoped<IWorkshopRepository, InMemoryWorkshopRepository>();
            services.AddScoped<ICollaboratorRepository, InMemoryCollaboratorRepository>();
            services.AddScoped<IAttendanceRecordRepository, InMemoryAttendanceRecordRepository>();
            return false;
        }
        if (provider != "MySql")
            throw new InvalidOperationException($"Persistence:Provider recebido '{provider}'; esperado InMemory ou MySql.");
        RegisterMySql(services, configuration.GetConnectionString("Workshops"));
        return true;
    }

    private static void RegisterMySql(IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Workshops recebida vazia; esperada conexão MySQL com Database definido.");
        try
        {
            var settings = new MySqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(settings.Database)) throw new ArgumentException();
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
        {
            throw new InvalidOperationException("ConnectionStrings:Workshops recebida inválida (valor omitido); esperada conexão MySQL com Database definido.");
        }
        services.AddSingleton(_ => new MySqlDataSource(connectionString));
        services.AddScoped<IWorkshopRepository, MySqlWorkshopRepository>();
        services.AddScoped<ICollaboratorRepository, MySqlCollaboratorRepository>();
        services.AddScoped<IAttendanceRecordRepository, MySqlAttendanceRecordRepository>();
    }

    /// <summary>Fails startup if MySQL or its schema is unavailable; never falls back to memory.</summary>
    public static void VerifyMySql(IServiceProvider services)
    {
        using var connection = services.GetRequiredService<MySqlDataSource>().OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT w.id, w.name, w.held_at, w.description, c.id, c.name, a.id, a.workshop_id, p.attendance_id, p.collaborator_id FROM workshops w, collaborators c, attendance_records a, attendance_participants p LIMIT 0";
        command.ExecuteNonQuery();
    }
}

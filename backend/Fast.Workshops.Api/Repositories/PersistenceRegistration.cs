using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Repositories.MySql;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

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
        services.AddDbContextFactory<WorkshopsDbContext>(options => options.UseMySQL(connectionString));
        services.AddScoped<IWorkshopRepository, MySqlWorkshopRepository>();
        services.AddScoped<ICollaboratorRepository, MySqlCollaboratorRepository>();
        services.AddScoped<IAttendanceRecordRepository, MySqlAttendanceRecordRepository>();
    }

    /// <summary>Fails startup if MySQL or its schema is unavailable; never falls back to memory.</summary>
    public static void VerifyMySql(IServiceProvider services)
    {
        using var context = services.GetRequiredService<IDbContextFactory<WorkshopsDbContext>>().CreateDbContext();
        _ = context.Set<WorkshopRow>().AsNoTracking().Take(1).ToArray();
        _ = context.Set<CollaboratorRow>().AsNoTracking().Take(1).ToArray();
        _ = context.Set<AttendanceRow>().AsNoTracking().Take(1).ToArray();
        _ = context.Set<ParticipantRow>().AsNoTracking().Take(1).ToArray();
    }
}

using System.Globalization;
using Fast.Workshops.Api.Models;
using MySqlConnector;

namespace Fast.Workshops.Api.Repositories.MySql;

public sealed class MySqlWorkshopRepository(MySqlDataSource connections) : IWorkshopRepository
{
    /// <inheritdoc />
    public Workshop Create(string name, DateTimeOffset heldAt, string description)
    {
        using var connection = connections.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO workshops (name, held_at, description) VALUES (@name, @heldAt, @description)";
        command.Parameters.AddWithValue("@name", name);
        // Preserve the original offset and all seven fractional digits for calendar-date filters.
        command.Parameters.AddWithValue("@heldAt", heldAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@description", description);
        command.ExecuteNonQuery();
        return new(checked((int)command.LastInsertedId), name, heldAt, description);
    }

    /// <inheritdoc />
    public Workshop? Find(int id) => ReadWorkshops(id).SingleOrDefault();

    /// <inheritdoc />
    public IReadOnlyList<Workshop> List() => ReadWorkshops(null);

    private IReadOnlyList<Workshop> ReadWorkshops(int? id)
    {
        using var connection = connections.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, held_at, description FROM workshops" + (id.HasValue ? " WHERE id = @id" : "");
        if (id.HasValue) command.Parameters.AddWithValue("@id", id.Value);
        using var reader = command.ExecuteReader();
        var workshops = new List<Workshop>();
        while (reader.Read())
            workshops.Add(new(reader.GetInt32(0), reader.GetString(1),
                DateTimeOffset.ParseExact(reader.GetString(2), "O", CultureInfo.InvariantCulture), reader.GetString(3)));
        return workshops;
    }
}

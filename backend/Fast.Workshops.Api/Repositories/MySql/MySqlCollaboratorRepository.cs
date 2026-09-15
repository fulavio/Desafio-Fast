using Fast.Workshops.Api.Models;
using MySqlConnector;

namespace Fast.Workshops.Api.Repositories.MySql;

public sealed class MySqlCollaboratorRepository(MySqlDataSource connections) : ICollaboratorRepository
{
    /// <inheritdoc />
    public Collaborator Create(string name)
    {
        using var connection = connections.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO collaborators (name) VALUES (@name)";
        command.Parameters.AddWithValue("@name", name);
        command.ExecuteNonQuery();
        return new(checked((int)command.LastInsertedId), name);
    }

    /// <inheritdoc />
    public Collaborator? Find(int id) => ReadCollaborators(id).SingleOrDefault();

    /// <inheritdoc />
    public IReadOnlyList<Collaborator> List() => ReadCollaborators(null);

    private IReadOnlyList<Collaborator> ReadCollaborators(int? id)
    {
        using var connection = connections.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM collaborators" + (id.HasValue ? " WHERE id = @id" : "");
        if (id.HasValue) command.Parameters.AddWithValue("@id", id.Value);
        using var reader = command.ExecuteReader();
        var collaborators = new List<Collaborator>();
        while (reader.Read()) collaborators.Add(new(reader.GetInt32(0), reader.GetString(1)));
        return collaborators;
    }
}

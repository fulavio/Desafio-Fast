using Fast.Workshops.Api.Models;
using MySqlConnector;

namespace Fast.Workshops.Api.Repositories.MySql;

public sealed class MySqlAttendanceRecordRepository(MySqlDataSource connections) : IAttendanceRecordRepository
{
    /// <inheritdoc />
    public AttendanceRecord Create(int workshopId)
    {
        using var connection = connections.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO attendance_records (workshop_id) VALUES (@workshopId)";
        command.Parameters.AddWithValue("@workshopId", workshopId);
        try
        {
            command.ExecuteNonQuery();
            return new(checked((int)command.LastInsertedId), workshopId);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        { throw new DuplicateAttendanceException($"Workshop '{workshopId}' já possui ata; esperado workshop sem ata."); }
        catch (MySqlException exception) when (exception.Number == 1452)
        { throw new MissingResourceException($"Workshop '{workshopId}' ausente; esperado ID existente."); }
    }

    /// <inheritdoc />
    public AttendanceRecord? Find(int id) => ReadAttendance(id).SingleOrDefault();

    /// <inheritdoc />
    public IReadOnlyList<AttendanceRecord> List() => ReadAttendance(null);

    /// <inheritdoc />
    public void AddCollaborator(int attendanceId, int collaboratorId)
    {
        using var connection = connections.OpenConnection();
        using var command = AssociationCommand(connection, attendanceId, collaboratorId);
        command.CommandText = "INSERT INTO attendance_participants (attendance_id, collaborator_id) VALUES (@attendanceId, @collaboratorId)";
        try { command.ExecuteNonQuery(); }
        catch (MySqlException exception) when (exception.Number == 1062)
        { /* The composite primary key makes repeated additions idempotent, including concurrent requests. */ }
        catch (MySqlException exception) when (exception.Number == 1452)
        { throw MissingAssociation(attendanceId, collaboratorId); }
    }

    /// <inheritdoc />
    public void RemoveCollaborator(int attendanceId, int collaboratorId)
    {
        using var connection = connections.OpenConnection();
        using var command = AssociationCommand(connection, attendanceId, collaboratorId);
        command.CommandText = "DELETE FROM attendance_participants WHERE attendance_id = @attendanceId AND collaborator_id = @collaboratorId";
        if (command.ExecuteNonQuery() == 0) throw MissingAssociation(attendanceId, collaboratorId);
    }

    private static MissingResourceException MissingAssociation(int attendanceId, int collaboratorId) =>
        new($"Associação '{attendanceId}/{collaboratorId}' ausente; esperados ata, colaborador e participação existentes.");

    private static MySqlCommand AssociationCommand(MySqlConnection connection, int attendanceId, int collaboratorId)
    {
        var command = connection.CreateCommand();
        command.Parameters.AddWithValue("@attendanceId", attendanceId);
        command.Parameters.AddWithValue("@collaboratorId", collaboratorId);
        return command;
    }

    private IReadOnlyList<AttendanceRecord> ReadAttendance(int? id)
    {
        using var connection = connections.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT a.id, a.workshop_id, p.collaborator_id FROM attendance_records a LEFT JOIN attendance_participants p ON p.attendance_id = a.id" +
            (id.HasValue ? " WHERE a.id = @id" : "");
        if (id.HasValue) command.Parameters.AddWithValue("@id", id.Value);
        using var reader = command.ExecuteReader();
        var records = new Dictionary<int, AttendanceRecord>();
        while (reader.Read())
        {
            var attendanceId = reader.GetInt32(0);
            if (!records.TryGetValue(attendanceId, out var record))
                records.Add(attendanceId, record = new(attendanceId, reader.GetInt32(1)));
            if (!reader.IsDBNull(2)) record.AddCollaborator(reader.GetInt32(2));
        }
        return records.Values.ToArray();
    }
}

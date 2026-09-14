using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Repositories.InMemory;

public sealed class InMemoryAttendanceRecordRepository(InMemoryDatabase database) : IAttendanceRecordRepository
{
    /// <inheritdoc />
    public AttendanceRecord Create(int workshopId)
    {
        lock (database.SyncRoot)
        {
            if (!database.Workshops.ContainsKey(workshopId))
                throw new MissingResourceException($"Workshop '{workshopId}' não encontrado; esperado ID existente.");
            if (database.AttendanceRecords.Values.Any(record => record.WorkshopId == workshopId))
                throw new DuplicateAttendanceException($"Workshop '{workshopId}' já possui ata; esperado workshop sem ata.");
            var attendance = new AttendanceRecord(++database.AttendanceSequence, workshopId);
            database.AttendanceRecords.Add(attendance.Id, attendance);
            return attendance.Snapshot();
        }
    }

    /// <inheritdoc />
    public AttendanceRecord? Find(int id)
    {
        lock (database.SyncRoot)
            return database.AttendanceRecords.GetValueOrDefault(id)?.Snapshot();
    }

    /// <inheritdoc />
    public IReadOnlyList<AttendanceRecord> List()
    {
        lock (database.SyncRoot)
            return database.AttendanceRecords.Values.Select(record => record.Snapshot()).ToArray();
    }

    /// <inheritdoc />
    public void AddCollaborator(int attendanceId, int collaboratorId)
    {
        lock (database.SyncRoot)
            RequireAssociationResources(attendanceId, collaboratorId).AddCollaborator(collaboratorId);
    }

    /// <inheritdoc />
    public void RemoveCollaborator(int attendanceId, int collaboratorId)
    {
        lock (database.SyncRoot)
        {
            if (!RequireAssociationResources(attendanceId, collaboratorId).RemoveCollaborator(collaboratorId))
                throw new MissingResourceException($"Associação '{attendanceId}/{collaboratorId}' ausente; esperada participação existente.");
        }
    }

    private AttendanceRecord RequireAssociationResources(int attendanceId, int collaboratorId)
    {
        if (!database.Collaborators.ContainsKey(collaboratorId))
            throw new MissingResourceException($"Colaborador '{collaboratorId}' ausente; esperado ID existente.");
        return database.AttendanceRecords.GetValueOrDefault(attendanceId)
            ?? throw new MissingResourceException($"Ata '{attendanceId}' ausente; esperado ID existente.");
    }
}

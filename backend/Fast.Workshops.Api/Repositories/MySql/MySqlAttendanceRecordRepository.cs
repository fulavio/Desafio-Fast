using Fast.Workshops.Api.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace Fast.Workshops.Api.Repositories.MySql;

public sealed class MySqlAttendanceRecordRepository(IDbContextFactory<WorkshopsDbContext> contexts) : IAttendanceRecordRepository
{
    /// <inheritdoc />
    public AttendanceRecord Create(int workshopId)
    {
        using var context = contexts.CreateDbContext();
        var attendance = new AttendanceRow { WorkshopId = workshopId };
        context.Add(attendance);
        try
        {
            context.SaveChanges();
            return new(attendance.Id, workshopId);
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 })
        { throw new DuplicateAttendanceException($"Workshop '{workshopId}' já possui ata; esperado workshop sem ata."); }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1452 })
        { throw new MissingResourceException($"Workshop '{workshopId}' ausente; esperado ID existente."); }
    }

    /// <inheritdoc />
    public AttendanceRecord? Find(int id) => ReadAttendance(id).SingleOrDefault();

    /// <inheritdoc />
    public IReadOnlyList<AttendanceRecord> List() => ReadAttendance(null);

    /// <inheritdoc />
    public void AddCollaborator(int attendanceId, int collaboratorId)
    {
        using var context = contexts.CreateDbContext();
        context.Add(new ParticipantRow { AttendanceId = attendanceId, CollaboratorId = collaboratorId });
        try { context.SaveChanges(); }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 })
        { /* The composite primary key makes repeated additions idempotent, including concurrent requests. */ }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1452 })
        { throw MissingAssociation(attendanceId, collaboratorId); }
    }

    /// <inheritdoc />
    public void RemoveCollaborator(int attendanceId, int collaboratorId)
    {
        using var context = contexts.CreateDbContext();
        var removed = context.Set<ParticipantRow>()
            .Where(row => row.AttendanceId == attendanceId && row.CollaboratorId == collaboratorId).ExecuteDelete();
        if (removed == 0) throw MissingAssociation(attendanceId, collaboratorId);
    }

    private static MissingResourceException MissingAssociation(int attendanceId, int collaboratorId) =>
        new($"Associação '{attendanceId}/{collaboratorId}' ausente; esperados ata, colaborador e participação existentes.");

    private IReadOnlyList<AttendanceRecord> ReadAttendance(int? id)
    {
        using var context = contexts.CreateDbContext();
        var records = context.Set<AttendanceRow>().AsNoTracking();
        if (id.HasValue) records = records.Where(row => row.Id == id.Value);
        return records.Include(row => row.Participants).AsSingleQuery().AsEnumerable().Select(ToSnapshot).ToArray();
    }

    private static AttendanceRecord ToSnapshot(AttendanceRow row)
    {
        var snapshot = new AttendanceRecord(row.Id, row.WorkshopId);
        foreach (var participant in row.Participants) snapshot.AddCollaborator(participant.CollaboratorId);
        return snapshot;
    }
}

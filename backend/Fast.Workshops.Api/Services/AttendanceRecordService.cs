using System.Globalization;
using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories;

namespace Fast.Workshops.Api.Services;

public sealed class AttendanceRecordService(IAttendanceRecordRepository attendanceRecords,
    IWorkshopRepository workshops, ICollaboratorRepository collaborators)
{
    /// <summary>Creates one attendance record for an existing workshop; e.g. workshop 1.</summary>
    public AttendanceResponse Create(CreateAttendanceRequest request)
    {
        InputRule.PositiveId(request.WorkshopId);
        var workshop = workshops.Find(request.WorkshopId)
            ?? throw new MissingResourceException($"Workshop '{request.WorkshopId}' ausente; esperado ID existente.");
        var attendance = attendanceRecords.Create(workshop.Id);
        return new(attendance.Id, ResponseProjection.Workshop(workshop), []);
    }

    /// <summary>Filters and sorts records; e.g. "code" and "2026-10-08" are combined with AND.</summary>
    public IReadOnlyList<AttendanceResponse> List(string? workshopName, string? calendarDate)
    {
        var date = ParseCalendarDate(calendarDate);
        var name = workshopName?.Trim() ?? "";
        // Read associations first: referenced resources are created before them and never deleted.
        var records = attendanceRecords.List();
        var sessions = workshops.List().ToDictionary(workshop => workshop.Id);
        var people = collaborators.List();
        return records
            .Select(record => new AttendanceResponse(record.Id, ResponseProjection.Workshop(sessions[record.WorkshopId]),
                ResponseProjection.Participants(record, people)))
            .Where(record => record.Workshop.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .Where(record => date is null || DateOnly.FromDateTime(record.Workshop.HeldAt.Date) == date)
            .OrderByDescending(record => record.Workshop.HeldAt)
            .ThenBy(record => record.Workshop.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.Id).ToArray();
    }

    /// <summary>Pages filtered summaries; e.g. page 1 returns six workshops with up to seven names each.</summary>
    public AttendancePageResponse ListPage(string? workshopName, string? calendarDate, string? collaboratorName, int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 50)
            throw new InputException($"pagina recebida '{page}', tamanhoPagina recebido '{pageSize}'; esperado pagina >= 1 e tamanhoPagina entre 1 e 50.");
        var name = collaboratorName?.Trim() ?? "";
        var matching = List(workshopName, calendarDate)
            .Where(record => name.Length == 0 || record.Collaborators.Any(person => person.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var offset = (long)(page - 1) * pageSize;
        var items = offset >= matching.Length ? [] : matching.Skip((int)offset).Take(pageSize)
            .Select(record => new AttendanceSummaryResponse(record.Id, record.Workshop,
                record.Collaborators.Take(7).ToArray(), record.Collaborators.Count)).ToArray();
        return new(items, matching.Length);
    }

    /// <summary>Adds an existing participant idempotently; e.g. record 1 and person 2.</summary>
    public void AddCollaborator(int attendanceId, int collaboratorId)
    {
        ValidateAssociation(attendanceId, collaboratorId);
        attendanceRecords.AddCollaborator(attendanceId, collaboratorId);
    }

    /// <summary>Removes an existing participation; e.g. record 1 and person 2.</summary>
    public void RemoveCollaborator(int attendanceId, int collaboratorId)
    {
        ValidateAssociation(attendanceId, collaboratorId);
        attendanceRecords.RemoveCollaborator(attendanceId, collaboratorId);
    }

    private void ValidateAssociation(int attendanceId, int collaboratorId)
    {
        InputRule.PositiveId(attendanceId);
        InputRule.PositiveId(collaboratorId);
        if (attendanceRecords.Find(attendanceId) is null)
            throw new MissingResourceException($"Ata '{attendanceId}' ausente; esperado ID existente.");
        if (collaborators.Find(collaboratorId) is null)
            throw new MissingResourceException($"Colaborador '{collaboratorId}' ausente; esperado ID existente.");
    }

    private static DateOnly? ParseCalendarDate(string? received)
    {
        if (received is null) return null;
        if (DateOnly.TryParseExact(received, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var calendarDate)) return calendarDate;
        throw new InputException($"data recebida '{received}'; esperado yyyy-MM-dd.");
    }
}

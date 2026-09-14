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
            .ThenBy(record => record.Workshop.Name, StringComparer.OrdinalIgnoreCase).ToArray();
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

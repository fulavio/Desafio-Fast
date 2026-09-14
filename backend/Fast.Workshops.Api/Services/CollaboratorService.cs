using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories;

namespace Fast.Workshops.Api.Services;

public sealed class CollaboratorService(ICollaboratorRepository collaborators,
    IWorkshopRepository workshops, IAttendanceRecordRepository attendanceRecords)
{
    /// <summary>Creates a collaborator after trimming the name; e.g. " Ana ".</summary>
    public CollaboratorResponse Create(CreateCollaboratorRequest request)
    {
        var name = InputRule.RequiredText(request.Name, "name");
        return ResponseProjection.Collaborator(collaborators.Create(name));
    }

    /// <summary>Lists people alphabetically with their attended workshops; e.g. Ana before Bruno.</summary>
    public IReadOnlyList<CollaboratorParticipationResponse> List()
    {
        var records = attendanceRecords.List();
        var sessions = workshops.List();
        return collaborators.List().OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
            .Select(person => new CollaboratorParticipationResponse(person.Id, person.Name,
                ParticipatedWorkshops(person.Id, sessions, records))).ToArray();
    }

    private static IReadOnlyList<WorkshopResponse> ParticipatedWorkshops(int collaboratorId,
        IReadOnlyList<Workshop> workshops, IReadOnlyList<AttendanceRecord> records)
    {
        var workshopIds = records.Where(record => record.CollaboratorIds.Contains(collaboratorId))
            .Select(record => record.WorkshopId).ToHashSet();
        return workshops.Where(workshop => workshopIds.Contains(workshop.Id))
            .OrderByDescending(workshop => workshop.HeldAt)
            .ThenBy(workshop => workshop.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ResponseProjection.Workshop).ToArray();
    }
}

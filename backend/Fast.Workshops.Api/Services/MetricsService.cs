using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Repositories;

namespace Fast.Workshops.Api.Services;

public sealed class MetricsService(ICollaboratorRepository collaborators,
    IWorkshopRepository workshops, IAttendanceRecordRepository attendanceRecords)
{
    /// <summary>Counts workshops for all collaborators; e.g. a person without attendance returns zero.</summary>
    public IReadOnlyList<CollaboratorWorkshopsCountResponse> CountWorkshops()
    {
        var counts = attendanceRecords.List()
            .SelectMany(record => record.CollaboratorIds.Select(id => (CollaboratorId: id, record.WorkshopId)))
            .Distinct().GroupBy(participation => participation.CollaboratorId)
            .ToDictionary(group => group.Key, group => group.Count());
        return collaborators.List().OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(person => person.Id)
            .Select(person => new CollaboratorWorkshopsCountResponse(person.Id, person.Name,
                counts.GetValueOrDefault(person.Id))).ToArray();
    }

    /// <summary>Counts participants for every workshop, including those without an attendance record.</summary>
    public IReadOnlyList<WorkshopCollaboratorsCountResponse> CountCollaborators()
    {
        var counts = attendanceRecords.List().ToDictionary(record => record.WorkshopId,
            record => record.CollaboratorIds.Count);
        return workshops.List().OrderBy(workshop => workshop.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(workshop => workshop.Id)
            .Select(workshop => new WorkshopCollaboratorsCountResponse(workshop.Id, workshop.Name,
                counts.GetValueOrDefault(workshop.Id))).ToArray();
    }
}

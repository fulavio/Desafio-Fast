using Fast.Workshops.Api.Contracts;

namespace Fast.Workshops.Api.Services;

public sealed class DevelopmentSeed(WorkshopService workshops, CollaboratorService collaborators,
    AttendanceRecordService attendanceRecords)
{
    /// <summary>Seeds the development host once at startup; e.g. three quarterly workshops.</summary>
    public void Populate()
    {
        var people = new[] { "Ana Souza", "Bruno Lima", "Carla Santos", "Diego Oliveira" }
            .Select(name => collaborators.Create(new(name))).ToArray();
        CreateSession(new("Clean Code", "2026-07-09T16:00:00-03:00",
            "Práticas para escrever código legível, simples e sustentável."), people.Take(3));
        CreateSession(new("Angular na prática", "2026-04-09T16:00:00-03:00",
            "Componentes, navegação e experiências acessíveis com Angular."), people.Skip(1));
        CreateSession(new("APIs com ASP.NET Core", "2026-01-08T16:00:00-03:00",
            "Contratos HTTP e boas práticas para construir APIs consistentes."), people.Take(2));
    }

    private void CreateSession(CreateWorkshopRequest request, IEnumerable<CollaboratorResponse> participants)
    {
        var workshop = workshops.Create(request);
        var attendance = attendanceRecords.Create(new(workshop.Id));
        foreach (var participant in participants)
            attendanceRecords.AddCollaborator(attendance.Id, participant.Id);
    }
}

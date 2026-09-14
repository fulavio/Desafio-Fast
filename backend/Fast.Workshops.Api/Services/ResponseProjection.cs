using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Services;

internal static class ResponseProjection
{
    internal static WorkshopResponse Workshop(Workshop workshop) =>
        new(workshop.Id, workshop.Name, workshop.HeldAt, workshop.Description);

    internal static CollaboratorResponse Collaborator(Collaborator collaborator) =>
        new(collaborator.Id, collaborator.Name);

    internal static IReadOnlyList<CollaboratorResponse> Participants(AttendanceRecord? attendance,
        IReadOnlyList<Collaborator> collaborators) =>
        collaborators.Where(person => attendance?.CollaboratorIds.Contains(person.Id) == true)
            .OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
            .Select(Collaborator).ToArray();
}

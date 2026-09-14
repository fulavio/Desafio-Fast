using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Repositories;

public interface ICollaboratorRepository
{
    /// <summary>Saves a collaborator with a generated ID; e.g. "Ana".</summary>
    Collaborator Create(string name);
    /// <summary>Returns a collaborator or null; e.g. Find(1).</summary>
    Collaborator? Find(int id);
    /// <summary>Returns a snapshot; e.g. for participant lookup.</summary>
    IReadOnlyList<Collaborator> List();
}

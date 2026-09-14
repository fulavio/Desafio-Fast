namespace Fast.Workshops.Api.Models;

public sealed class Collaborator
{
    public int Id { get; }
    public string Name { get; }

    /// <summary>Creates a collaborator; e.g. ID 1 and "Ana".</summary>
    public Collaborator(int id, string name)
    {
        Id = InputRule.PositiveId(id);
        Name = InputRule.RequiredText(name, "name");
    }
}

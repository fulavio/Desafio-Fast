namespace Fast.Workshops.Api.Models;

public sealed class Workshop
{
    public int Id { get; }
    public string Name { get; }
    public DateTimeOffset HeldAt { get; }
    public string Description { get; }

    /// <summary>Creates a valid workshop; e.g. ID 1, "Clean Code", a timestamp and description.</summary>
    public Workshop(int id, string name, DateTimeOffset heldAt, string description)
    {
        Id = InputRule.PositiveId(id);
        Name = InputRule.RequiredText(name, "name");
        HeldAt = heldAt;
        Description = InputRule.RequiredText(description, "description");
    }
}

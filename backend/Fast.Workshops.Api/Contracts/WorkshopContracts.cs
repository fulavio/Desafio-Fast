namespace Fast.Workshops.Api.Contracts;

public sealed record CreateWorkshopRequest(string? Name, string? HeldAt, string? Description);
public sealed record WorkshopResponse(int Id, string Name, DateTimeOffset HeldAt, string Description);

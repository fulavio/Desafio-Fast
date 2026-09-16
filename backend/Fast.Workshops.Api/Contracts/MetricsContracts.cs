namespace Fast.Workshops.Api.Contracts;

public sealed record CollaboratorWorkshopsCountResponse(int CollaboratorId, string Name, int WorkshopsCount);
public sealed record WorkshopCollaboratorsCountResponse(int WorkshopId, string Name, int CollaboratorsCount);

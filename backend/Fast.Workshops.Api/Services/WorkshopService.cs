using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Models;
using Fast.Workshops.Api.Repositories;

namespace Fast.Workshops.Api.Services;

public sealed class WorkshopService(IWorkshopRepository workshops)
{
    /// <summary>Validates and creates a workshop; e.g. a Clean Code request.</summary>
    public WorkshopResponse Create(CreateWorkshopRequest request)
    {
        var name = InputRule.RequiredText(request.Name, "name");
        var description = InputRule.RequiredText(request.Description, "description");
        var heldAt = InputRule.Timestamp(request.HeldAt);
        return ResponseProjection.Workshop(workshops.Create(name, heldAt, description));
    }

}

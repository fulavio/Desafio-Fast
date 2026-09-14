using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fast.Workshops.Api.Controllers;

[ApiController]
[Route("api/workshops")]
public sealed class WorkshopsController(WorkshopService workshops) : ControllerBase
{
    /// <summary>POST /api/workshops creates a workshop; e.g. a valid JSON request returns 201.</summary>
    [HttpPost]
    public ActionResult<WorkshopResponse> Create(CreateWorkshopRequest request)
    {
        var workshop = workshops.Create(request);
        return Created("/api/workshops", workshop);
    }

}

using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fast.Workshops.Api.Controllers;

[ApiController]
[Route("api/colaboradores")]
public sealed class CollaboratorsController(CollaboratorService collaborators) : ControllerBase
{
    /// <summary>POST /api/colaboradores creates a person; e.g. {"name":"Ana"}.</summary>
    [HttpPost]
    public ActionResult<CollaboratorResponse> Create(CreateCollaboratorRequest request)
    {
        var collaborator = collaborators.Create(request);
        return Created("/api/colaboradores", collaborator);
    }

    /// <summary>GET /api/colaboradores lists people and their workshops alphabetically.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<CollaboratorParticipationResponse>> List() => Ok(collaborators.List());
}

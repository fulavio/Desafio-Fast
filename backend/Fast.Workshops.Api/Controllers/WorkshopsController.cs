using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Fast.Workshops.Api.Controllers;

[ApiController]
[Route("api/workshops")]
public sealed class WorkshopsController(WorkshopService workshops) : ControllerBase
{
    /// <summary>POST /api/workshops creates a workshop; e.g. a valid JSON request returns 201.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Cadastrar workshop", Description = "Nome e descrição são obrigatórios. heldAt usa ISO 8601 com horário e fuso, por exemplo 2026-10-08T16:00:00-03:00. Espaços externos são removidos dos textos.")]
    [ProducesResponseType(typeof(WorkshopResponse), 201, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    public ActionResult<WorkshopResponse> Create(CreateWorkshopRequest request)
    {
        var workshop = workshops.Create(request);
        return Created("/api/workshops", workshop);
    }

}

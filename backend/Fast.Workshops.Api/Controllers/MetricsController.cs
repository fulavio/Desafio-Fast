using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Fast.Workshops.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController(MetricsService metrics) : ControllerBase
{
    /// <summary>GET /api/metrics/colaboradores/workshops-count returns all collaborator counts, including zeros.</summary>
    [HttpGet("colaboradores/workshops-count")]
    [SwaggerOperation(Summary = "Total de workshops por colaborador", Description = "Conta workshops distintos para todos os colaboradores, incluindo zero para quem não tem presença. Ordena por total decrescente, nome e ID; banco vazio retorna [].")]
    [ProducesResponseType(typeof(IReadOnlyList<CollaboratorWorkshopsCountResponse>), 200)]
    public ActionResult<IReadOnlyList<CollaboratorWorkshopsCountResponse>> CountWorkshops() => Ok(metrics.CountWorkshops());

    /// <summary>GET /api/metrics/workshops/colaboradores-count returns all workshop counts, including zeros.</summary>
    [HttpGet("workshops/colaboradores-count")]
    [SwaggerOperation(Summary = "Total de colaboradores por workshop", Description = "Inclui workshops sem ata ou participantes com zero. Ordena por total decrescente, nome e ID; banco vazio retorna [].")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkshopCollaboratorsCountResponse>), 200)]
    public ActionResult<IReadOnlyList<WorkshopCollaboratorsCountResponse>> CountCollaborators() => Ok(metrics.CountCollaborators());
}

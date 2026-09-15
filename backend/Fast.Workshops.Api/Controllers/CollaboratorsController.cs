using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Fast.Workshops.Api.Controllers;

[ApiController]
[Route("api/colaboradores")]
public sealed class CollaboratorsController(CollaboratorService collaborators) : ControllerBase
{
    /// <summary>POST /api/colaboradores creates a person; e.g. {"name":"Ana"}.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Cadastrar colaborador", Description = "Nome obrigatório, com espaços externos removidos. Nomes repetidos são permitidos.")]
    [ProducesResponseType(typeof(CollaboratorResponse), 201, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    public ActionResult<CollaboratorResponse> Create(CreateCollaboratorRequest request)
    {
        var collaborator = collaborators.Create(request);
        return Created("/api/colaboradores", collaborator);
    }

    /// <summary>GET /api/colaboradores lists people and their workshops alphabetically.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Listar colaboradores e participações", Description = "Retorna colaboradores em ordem alfabética com os workshops em que participaram. Sem resultados, retorna uma lista vazia.")]
    [ProducesResponseType(typeof(IReadOnlyList<CollaboratorParticipationResponse>), 200, "application/json")]
    public ActionResult<IReadOnlyList<CollaboratorParticipationResponse>> List() => Ok(collaborators.List());
}

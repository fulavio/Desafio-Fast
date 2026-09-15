using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Fast.Workshops.Api.Controllers;

[ApiController]
[Route("api/atas")]
public sealed class AttendanceRecordsController(AttendanceRecordService attendanceRecords) : ControllerBase
{
    /// <summary>POST /api/atas creates a record; e.g. {"workshopId":1}.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Criar ata de presença", Description = "workshopId deve ser um inteiro positivo de um workshop existente. Cada workshop pode ter uma única ata, criada sem participantes.")]
    [ProducesResponseType(typeof(AttendanceResponse), 201, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), 404, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), 409, "application/problem+json")]
    public ActionResult<AttendanceResponse> Create(CreateAttendanceRequest request)
    {
        var attendance = attendanceRecords.Create(request);
        return Created("/api/atas", attendance);
    }

    /// <summary>GET /api/atas?workshopNome=code&amp;data=2026-10-08 filters attendance.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Consultar atas de presença", Description = "Combina filtros por AND. Ordena por data decrescente e nome do workshop no desempate; participantes em ordem alfabética. Sem resultados, retorna [].")]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceResponse>), 200, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    public ActionResult<IReadOnlyList<AttendanceResponse>> List(
        [FromQuery(Name = "workshopNome"), SwaggerParameter("Parte do nome, sem diferenciar maiúsculas e minúsculas; espaços externos são removidos.")] string? workshopName,
        [FromQuery(Name = "data"), SwaggerParameter("Data exata em yyyy-MM-dd, no fuso armazenado. Exemplo: 2026-10-08. Omita para não filtrar; valor vazio é inválido.")] string? calendarDate) =>
        Ok(attendanceRecords.List(workshopName, Request.Query.ContainsKey("data") ? calendarDate ?? "" : null));

    /// <summary>GET /api/atas/pagina?pagina=1&amp;tamanhoPagina=6 loads workshop summaries.</summary>
    [HttpGet("pagina")]
    [SwaggerOperation(Summary = "Consultar página de atas", Description = "Filtra por workshop, data e colaborador antes de paginar. Retorna items e total, com até sete participantes e participantCount por ata. Ordena por data decrescente, nome e ID para desempate.")]
    [ProducesResponseType(typeof(AttendancePageResponse), 200, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    public ActionResult<AttendancePageResponse> ListPage(
        [FromQuery(Name = "workshopNome")] string? workshopName,
        [FromQuery(Name = "data")] string? calendarDate,
        [FromQuery(Name = "colaborador")] string? collaboratorName,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "tamanhoPagina")] int pageSize = 6)
    {
        var date = Request.Query.ContainsKey("data") ? calendarDate ?? "" : null;
        return Ok(attendanceRecords.ListPage(workshopName, date, collaboratorName, page, pageSize));
    }

    /// <summary>PUT /api/atas/1/colaboradores/2 adds the participant idempotently.</summary>
    [HttpPut("{ataId}/colaboradores/{colaboradorId}")]
    [SwaggerOperation(Summary = "Adicionar participante à ata", Description = "IDs devem ser inteiros positivos e existentes. Não recebe corpo. Adicionar a mesma pessoa novamente mantém uma única participação e retorna 204.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), 404, "application/problem+json")]
    public IActionResult AddCollaborator(int ataId, int colaboradorId)
    {
        attendanceRecords.AddCollaborator(ataId, colaboradorId);
        return NoContent();
    }

    /// <summary>DELETE /api/atas/1/colaboradores/2 removes an existing participant.</summary>
    [HttpDelete("{ataId}/colaboradores/{colaboradorId}")]
    [SwaggerOperation(Summary = "Remover participante da ata", Description = "IDs devem ser inteiros positivos. Remove somente a participação, preservando o cadastro do colaborador. Não recebe corpo. Ata, colaborador ou associação inexistente retorna 404.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), 404, "application/problem+json")]
    public IActionResult RemoveCollaborator(int ataId, int colaboradorId)
    {
        attendanceRecords.RemoveCollaborator(ataId, colaboradorId);
        return NoContent();
    }
}

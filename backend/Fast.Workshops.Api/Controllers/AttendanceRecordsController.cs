using Fast.Workshops.Api.Contracts;
using Fast.Workshops.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fast.Workshops.Api.Controllers;

[ApiController]
[Route("api/atas")]
public sealed class AttendanceRecordsController(AttendanceRecordService attendanceRecords) : ControllerBase
{
    /// <summary>POST /api/atas creates a record; e.g. {"workshopId":1}.</summary>
    [HttpPost]
    public ActionResult<AttendanceResponse> Create(CreateAttendanceRequest request)
    {
        var attendance = attendanceRecords.Create(request);
        return Created("/api/atas", attendance);
    }

    /// <summary>GET /api/atas?workshopNome=code&amp;data=2026-10-08 filters attendance.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<AttendanceResponse>> List(
        [FromQuery(Name = "workshopNome")] string? workshopName, [FromQuery(Name = "data")] string? calendarDate) =>
        Ok(attendanceRecords.List(workshopName, Request.Query.ContainsKey("data") ? calendarDate ?? "" : null));

    /// <summary>PUT /api/atas/1/colaboradores/2 adds the participant idempotently.</summary>
    [HttpPut("{ataId}/colaboradores/{colaboradorId}")]
    public IActionResult AddCollaborator(int ataId, int colaboradorId)
    {
        attendanceRecords.AddCollaborator(ataId, colaboradorId);
        return NoContent();
    }

    /// <summary>DELETE /api/atas/1/colaboradores/2 removes an existing participant.</summary>
    [HttpDelete("{ataId}/colaboradores/{colaboradorId}")]
    public IActionResult RemoveCollaborator(int ataId, int colaboradorId)
    {
        attendanceRecords.RemoveCollaborator(ataId, colaboradorId);
        return NoContent();
    }
}

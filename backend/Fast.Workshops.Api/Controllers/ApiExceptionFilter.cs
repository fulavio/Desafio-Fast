using Fast.Workshops.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fast.Workshops.Api.Controllers;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    /// <summary>Maps known rule failures to ProblemDetails; e.g. duplicate attendance returns 409.</summary>
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch
        {
            InputException => 400,
            MissingResourceException => 404,
            DuplicateAttendanceException => 409,
            _ => 0
        };
        if (status == 0) return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = "Não foi possível concluir a solicitação.",
            Detail = context.Exception.Message
        })
        { StatusCode = status, ContentTypes = { "application/problem+json" } };
        context.ExceptionHandled = true;
    }
}

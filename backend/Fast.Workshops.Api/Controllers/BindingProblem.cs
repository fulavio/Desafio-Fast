using Microsoft.AspNetCore.Mvc;

namespace Fast.Workshops.Api.Controllers;

internal static class BindingProblem
{
    internal static IActionResult Create(ActionContext context)
    {
        var details = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
            .Select(entry => $"Campo '{entry.Key}', valor '{entry.Value?.AttemptedValue ?? "JSON inválido ou incompatível"}'; esperado JSON válido e IDs inteiros positivos.");
        return new BadRequestObjectResult(new ProblemDetails
        {
            Status = 400,
            Title = "Solicitação inválida.",
            Detail = string.Join(" ", details)
        })
        { ContentTypes = { "application/problem+json" } };
    }
}

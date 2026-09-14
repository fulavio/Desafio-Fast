import { HttpErrorResponse } from "@angular/common/http";

/** Produces safe, actionable UI text; e.g. status 0 asks to check the API connection. */
export function apiErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse))
    return "Não foi possível carregar. Tente novamente.";
  if (error.status === 0)
    return "Não foi possível conectar à API. Verifique se o backend está em execução.";
  if (error.status === 404)
    return "Workshop não encontrado. Volte para a lista de atas.";
  return "Não foi possível carregar. Verifique os filtros e tente novamente.";
}

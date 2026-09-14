import { HttpErrorResponse } from "@angular/common/http";
import { apiErrorMessage } from "./api-error";

describe("API errors", () => {
  it("explains connection errors without exposing response internals", () => {
    expect(apiErrorMessage(new HttpErrorResponse({ status: 0 }))).toContain(
      "backend",
    );
    expect(apiErrorMessage(new HttpErrorResponse({ status: 404 }))).toContain(
      "não encontrado",
    );
    expect(
      apiErrorMessage(
        new HttpErrorResponse({ status: 500, error: "secret stack" }),
      ),
    ).not.toContain("secret");
    expect(apiErrorMessage(new Error("secret"))).toContain("Tente novamente");
  });
});

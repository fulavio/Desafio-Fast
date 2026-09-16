import { HttpTestingController } from "@angular/common/http/testing";
import { environment } from "../environments/environment";

/** Owns the aggregate HTTP boundary; e.g. each chart requires exactly one request. */
export class FakeMetricsServer {
  constructor(private readonly http: HttpTestingController) {}

  /** Supplies all person totals, including zeros and duplicate names; e.g. two people named Ana. */
  people(zero = false, empty = false): void {
    const request = this.http.expectOne(
      environment.apiUrl + "/metrics/colaboradores/workshops-count",
    );
    expect(request.request.method).toBe("GET");
    request.flush(
      empty
        ? []
        : [
            { collaboratorId: 2, name: "Ana", workshopsCount: zero ? 0 : 17 },
            { collaboratorId: 9, name: "Ana", workshopsCount: 0 },
          ],
    );
  }

  /** Includes a workshop with zero participants; e.g. useful in the accessible table. */
  workshops(zero = false, empty = false): void {
    const request = this.http.expectOne(
      environment.apiUrl + "/metrics/workshops/colaboradores-count",
    );
    expect(request.request.method).toBe("GET");
    request.flush(
      empty
        ? []
        : [
            {
              workshopId: 3,
              name: "Angular",
              collaboratorsCount: zero ? 0 : 8,
            },
            { workshopId: 4, name: "Code", collaboratorsCount: 0 },
          ],
    );
  }

  /** Simulates an unavailable aggregate endpoint; e.g. retry after HTTP 503. */
  fail(path: string): void {
    this.http
      .expectOne(environment.apiUrl + path)
      .flush({}, { status: 503, statusText: "Unavailable" });
  }
}

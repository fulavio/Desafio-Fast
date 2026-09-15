import { HttpTestingController } from "@angular/common/http/testing";
import { environment } from "../environments/environment";
import { AttendanceRecord } from "../app/core/models/workshop.models";

export const sampleAttendance: readonly AttendanceRecord[] = [
  {
    id: 1,
    workshop: {
      id: 1,
      name: "Clean Code",
      heldAt: "2026-07-09T23:30:00-03:00",
      description: "Código legível.",
    },
    collaborators: [
      { id: 1, name: "Ana Souza" },
      { id: 2, name: "Bruno Lima" },
    ],
  },
  {
    id: 2,
    workshop: {
      id: 2,
      name: "Angular",
      heldAt: "2026-04-09T16:00:00-03:00",
      description: "Componentes acessíveis.",
    },
    collaborators: [{ id: 2, name: "Bruno Lima" }],
  },
];

/** Owns the HTTP test boundary; e.g. replyAttendance supplies isolated records without a network. */
export class FakeWorkshopServer {
  constructor(private readonly http: HttpTestingController) {}

  /** Supplies a page after checking all remote filters; e.g. page 2 of 20 records. */
  replyAttendancePage(
    records: readonly AttendanceRecord[] = sampleAttendance,
    name = "",
    date = "",
    collaborator = "",
    page = 1,
    total = records.length,
  ): void {
    const request = this.http.expectOne(
      (request) => request.url === environment.apiUrl + "/atas/pagina",
    );
    expect(request.request.method).toBe("GET");
    expect(request.request.params.get("workshopNome") ?? "").toBe(name);
    expect(request.request.params.get("data") ?? "").toBe(date);
    expect(request.request.params.get("colaborador") ?? "").toBe(collaborator);
    expect(request.request.params.get("pagina")).toBe(String(page));
    expect(request.request.params.get("tamanhoPagina")).toBe("6");
    request.flush({
      items: records.map((record) => ({
        ...record,
        collaborators: record.collaborators.slice(0, 7),
        participantCount: record.collaborators.length,
      })),
      total,
    });
  }

  /** Answers the attendance request and checks remote filters; e.g. code on July 9. */
  replyAttendance(
    records: readonly AttendanceRecord[] = sampleAttendance,
    name = "",
    date = "",
  ): void {
    const request = this.http.expectOne(
      (request) => request.url === environment.apiUrl + "/atas",
    );
    expect(request.request.method).toBe("GET");
    expect(request.request.params.get("workshopNome") ?? "").toBe(name);
    expect(request.request.params.get("data") ?? "").toBe(date);
    expect(request.request.params.has("colaborador")).toBe(false);
    request.flush(records);
  }

  /** Answers a details lookup through attendance; e.g. workshop 1 has attendance 8. */
  replyDetails(detail: AttendanceRecord = sampleAttendance[0]): void {
    this.replyAttendance([detail]);
  }

  /** Produces a controlled HTTP error; e.g. 503 followed by a successful retry. */
  fail(path: string, status = 503): void {
    this.http
      .expectOne((request) => request.url === environment.apiUrl + path)
      .flush({ title: "Unavailable" }, { status, statusText: "Failure" });
  }

  /** Verifies cancellation of stale filters; e.g. an earlier list cannot overwrite the latest search. */
  expectCancelledAttendance(path = "/atas"): void {
    const request = this.http.expectOne(environment.apiUrl + path);
    expect(request.cancelled).toBe(true);
  }

  /** Completes a DELETE without removing the collaborator's registration; e.g. attendance 8, person 1. */
  replyRemoval(attendanceId: number, collaboratorId: number): void {
    const request = this.http.expectOne(
      environment.apiUrl +
        "/atas/" +
        attendanceId +
        "/colaboradores/" +
        collaboratorId,
    );
    expect(request.request.method).toBe("DELETE");
    expect(request.request.body).toBeNull();
    request.flush(null, { status: 204, statusText: "No Content" });
  }
}

import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { map, Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  AttendanceFilters,
  AttendancePage,
  AttendanceRecord,
} from "../models/workshop.models";

@Injectable({ providedIn: "root" })
export class WorkshopsApiService {
  private readonly http: HttpClient;

  constructor() {
    this.http = inject(HttpClient);
  }

  /** Loads remote filters; e.g. workshopNome=code and data=2026-07-09. */
  listAttendance(
    filters: AttendanceFilters,
  ): Observable<readonly AttendanceRecord[]> {
    let params = new HttpParams();
    if (filters.workshopNome)
      params = params.set("workshopNome", filters.workshopNome);
    if (filters.data) params = params.set("data", filters.data);
    return this.http.get<readonly AttendanceRecord[]>(
      environment.apiUrl + "/atas",
      { params },
    );
  }

  /** Loads six summaries after all filters; e.g. page 2 contains the next matching workshops. */
  attendancePage(
    filters: AttendanceFilters,
    page: number,
  ): Observable<AttendancePage> {
    let params = new HttpParams().set("pagina", page).set("tamanhoPagina", 6);
    if (filters.workshopNome)
      params = params.set("workshopNome", filters.workshopNome);
    if (filters.data) params = params.set("data", filters.data);
    if (filters.colaborador)
      params = params.set("colaborador", filters.colaborador);
    return this.http.get<AttendancePage>(environment.apiUrl + "/atas/pagina", {
      params,
    });
  }

  /** Finds a workshop's attendance using the existing list; e.g. workshop 1 may have attendance 8. */
  workshopAttendance(id: number): Observable<AttendanceRecord | null> {
    return this.listAttendance({
      workshopNome: "",
      data: "",
      colaborador: "",
    }).pipe(
      map(
        (records) =>
          records.find((record) => record.workshop.id === id) ?? null,
      ),
    );
  }

  /** Removes only the participation; e.g. collaborator 2 from attendance 8. */
  removeParticipant(
    attendanceId: number,
    collaboratorId: number,
  ): Observable<void> {
    return this.http.delete<void>(
      environment.apiUrl +
        "/atas/" +
        attendanceId +
        "/colaboradores/" +
        collaboratorId,
    );
  }
}

import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";

export interface CollaboratorWorkshopsCount {
  readonly collaboratorId: number;
  readonly name: string;
  readonly workshopsCount: number;
}

export interface WorkshopCollaboratorsCount {
  readonly workshopId: number;
  readonly name: string;
  readonly collaboratorsCount: number;
}

@Injectable({ providedIn: "root" })
export class MetricsApiService {
  private readonly http: HttpClient;

  constructor() {
    this.http = inject(HttpClient);
  }

  /** Fetches all collaborator totals in one request; e.g. Ana and Bruno including zeros. */
  collaboratorCounts(): Observable<readonly CollaboratorWorkshopsCount[]> {
    return this.http.get<readonly CollaboratorWorkshopsCount[]>(
      environment.apiUrl + "/metrics/colaboradores/workshops-count",
    );
  }

  /** Fetches all workshop totals, including zeros; e.g. a workshop without attendance. */
  workshopCounts(): Observable<readonly WorkshopCollaboratorsCount[]> {
    return this.http.get<readonly WorkshopCollaboratorsCount[]>(
      environment.apiUrl + "/metrics/workshops/colaboradores-count",
    );
  }
}

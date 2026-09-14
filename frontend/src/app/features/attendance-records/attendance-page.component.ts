import { Component, computed, DestroyRef, inject, signal } from "@angular/core";
import { DatePipe } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, ParamMap, Router, RouterLink } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { BehaviorSubject, combineLatest, map, switchMap } from "rxjs";
import { WorkshopsApiService } from "../../core/api/workshops-api.service";
import {
  AttendanceFilters,
  AttendanceRecord,
} from "../../core/models/workshop.models";
import { requestState, RequestState } from "../../core/api/request-state";
import { LoadingStateComponent } from "../../shared/loading-state/loading-state.component";
import { EmptyStateComponent } from "../../shared/empty-state/empty-state.component";
import { ErrorStateComponent } from "../../shared/error-state/error-state.component";

const emptyFilters: AttendanceFilters = {
  workshopNome: "",
  data: "",
  colaborador: "",
};

@Component({
  selector: "app-attendance-page",
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: "./attendance-page.component.html",
})
export class AttendancePageComponent {
  readonly view = signal<RequestState<readonly AttendanceRecord[]>>({
    value: [],
    loading: true,
    error: "",
  });
  readonly appliedFilters = signal<AttendanceFilters>(emptyFilters);
  filterDraft: AttendanceFilters = { ...emptyFilters };
  private readonly refresh = new BehaviorSubject<number>(0);
  readonly visibleRecords = computed(() => {
    const name = this.appliedFilters().colaborador.toLocaleLowerCase("pt-BR");
    return this.view().value.filter(
      (record) =>
        !name ||
        record.collaborators.some((person) =>
          person.name.toLocaleLowerCase("pt-BR").includes(name),
        ),
    );
  });

  private readonly api: WorkshopsApiService;
  private readonly route: ActivatedRoute;
  private readonly router: Router;

  constructor() {
    this.api = inject(WorkshopsApiService);
    this.route = inject(ActivatedRoute);
    this.router = inject(Router);
    this.observeFilters(inject(DestroyRef));
  }

  private observeFilters(destroyRef: DestroyRef): void {
    combineLatest([this.route.queryParamMap, this.refresh])
      .pipe(
        map(([params]) => this.readFilters(params)),
        switchMap((filters) =>
          requestState(this.api.listAttendance(filters), []),
        ),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((view) => this.view.set(view));
  }

  /** Applies the visible form to the URL; e.g. colaborador=Ana remains a client filter. */
  applyFilters(): void {
    const filters = this.filterDraft;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        workshopNome: filters.workshopNome.trim() || null,
        data: filters.data || null,
        colaborador: filters.colaborador.trim() || null,
      },
    });
  }

  /** Clears all filters; e.g. a search returns to /atas. */
  clearFilters(): void {
    this.filterDraft = { ...emptyFilters };
    this.applyFilters();
  }

  /** Retries the current request; e.g. after the backend becomes available. */
  retry(): void {
    this.refresh.next(this.refresh.value + 1);
  }

  private readFilters(params: ParamMap): AttendanceFilters {
    const filters = {
      workshopNome: params.get("workshopNome")?.trim() ?? "",
      data: params.get("data") ?? "",
      colaborador: params.get("colaborador")?.trim() ?? "",
    };
    this.filterDraft = { ...filters };
    this.appliedFilters.set(filters);
    return filters;
  }
}

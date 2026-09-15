import { Component, computed, DestroyRef, inject, signal } from "@angular/core";
import { DatePipe } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, ParamMap, Router, RouterLink } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  BehaviorSubject,
  combineLatest,
  exhaustMap,
  map,
  Observable,
  startWith,
  Subject,
  switchMap,
} from "rxjs";
import { WorkshopsApiService } from "../../core/api/workshops-api.service";
import {
  AttendanceFilters,
  AttendancePage,
  AttendanceSummary,
} from "../../core/models/workshop.models";
import { requestState, RequestState } from "../../core/api/request-state";
import { LoadingStateComponent } from "../../shared/loading-state/loading-state.component";
import { EmptyStateComponent } from "../../shared/empty-state/empty-state.component";
import { ErrorStateComponent } from "../../shared/error-state/error-state.component";
import { LoadMoreDirective } from "./load-more.directive";

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
    LoadMoreDirective,
  ],
  templateUrl: "./attendance-page.component.html",
  styleUrl: "./attendance-page.component.scss",
})
export class AttendancePageComponent {
  readonly view = signal<RequestState<readonly AttendanceSummary[]>>({
    value: [],
    loading: true,
    error: "",
  });
  filterDraft: AttendanceFilters = { ...emptyFilters };
  private readonly refresh = new BehaviorSubject<number>(0);
  private readonly pageRequests = new Subject<number>();
  private nextPage = 1;
  readonly total = signal(0);
  readonly visibleRecords = computed(() => this.view().value);
  readonly hasMore = computed(
    () => this.visibleRecords().length < this.total(),
  );

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
        switchMap((filters) => this.requestPages(filters)),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((view) => this.receivePage(view));
  }

  private requestPages(
    filters: AttendanceFilters,
  ): Observable<RequestState<AttendancePage | null>> {
    return this.pageRequests.pipe(
      startWith(1),
      exhaustMap((page) =>
        requestState<AttendancePage | null>(
          this.api.attendancePage(filters, page),
          null,
        ),
      ),
    );
  }

  private receivePage(page: RequestState<AttendancePage | null>): void {
    const existing = this.view().value;
    this.view.set({
      ...page,
      value: page.value ? [...existing, ...page.value.items] : existing,
    });
    if (!page.value) return;
    this.total.set(page.value.total);
    this.nextPage += 1;
  }

  /** Requests the next six workshops; e.g. scrolling to the end or pressing Carregar mais. */
  loadMore(): void {
    if (this.view().loading || !this.hasMore()) return;
    this.pageRequests.next(this.nextPage);
  }

  /** Applies the visible form to the URL; e.g. colaborador=Ana filters before pagination. */
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
    if (this.visibleRecords().length > 0) {
      this.loadMore();
      return;
    }
    this.refresh.next(this.refresh.value + 1);
  }

  private readFilters(params: ParamMap): AttendanceFilters {
    const filters = {
      workshopNome: params.get("workshopNome")?.trim() ?? "",
      data: params.get("data") ?? "",
      colaborador: params.get("colaborador")?.trim() ?? "",
    };
    this.filterDraft = { ...filters };
    this.nextPage = 1;
    this.total.set(0);
    this.view.set({ value: [], loading: true, error: "" });
    return filters;
  }
}

import { Component, DestroyRef, computed, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { RouterLink } from "@angular/router";
import { BehaviorSubject, switchMap } from "rxjs";
import { BaseChartDirective, provideCharts } from "ng2-charts";
import {
  ArcElement,
  BarController,
  BarElement,
  CategoryScale,
  LinearScale,
  PieController,
  Tooltip,
  Colors,
  Legend,
  ChartData,
  ChartOptions,
} from "chart.js";
import {
  MetricsApiService,
  CollaboratorWorkshopsCount,
  WorkshopCollaboratorsCount,
} from "../../core/api/metrics-api.service";
import { requestState, RequestState } from "../../core/api/request-state";
import { LoadingStateComponent } from "../../shared/loading-state/loading-state.component";
import { ErrorStateComponent } from "../../shared/error-state/error-state.component";

@Component({
  selector: "app-metrics-page",
  imports: [
    RouterLink,
    BaseChartDirective,
    LoadingStateComponent,
    ErrorStateComponent,
  ],
  providers: [
    provideCharts({
      registerables: [
        BarController,
        BarElement,
        CategoryScale,
        LinearScale,
        PieController,
        ArcElement,
        Tooltip,
        Colors,
        Legend,
      ],
    }),
  ],
  templateUrl: "./metrics-page.component.html",
  styleUrl: "./metrics-page.component.scss",
})
export class MetricsPageComponent {
  readonly people = signal<RequestState<readonly CollaboratorWorkshopsCount[]>>(
    { value: [], loading: true, error: "" },
  );
  readonly workshops = signal<
    RequestState<readonly WorkshopCollaboratorsCount[]>
  >({ value: [], loading: true, error: "" });
  private readonly refresh = new BehaviorSubject<number>(0);
  readonly barOptions: ChartOptions<"bar"> = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    indexAxis: "y",
    plugins: { legend: { display: false } },
    scales: { x: { beginAtZero: true, ticks: { precision: 0 } } },
  };
  readonly pieOptions: ChartOptions<"pie"> = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    plugins: {
      legend: {
        position: "bottom",
        labels: {
          sort: (first, second, chart): number => {
            const totals = chart.datasets[0]?.data ?? [];
            return (
              Number(totals[second.index ?? -1] ?? 0) -
              Number(totals[first.index ?? -1] ?? 0)
            );
          },
        },
      },
    },
  };
  readonly barChart = computed<ChartData<"bar">>(() => ({
    labels: this.people().value.map((person) => person.name),
    datasets: [
      {
        label: "Workshops",
        data: this.people().value.map((person) => person.workshopsCount),
        backgroundColor: "#174d3f",
      },
    ],
  }));
  readonly pieChart = computed<ChartData<"pie">>(() => ({
    labels: this.workshops().value.map((workshop) => workshop.name),
    datasets: [
      {
        label: "Colaboradores",
        data: this.workshops().value.map(
          (workshop) => workshop.collaboratorsCount,
        ),
      },
    ],
  }));
  readonly hasParticipants = computed(() =>
    this.workshops().value.some((workshop) => workshop.collaboratorsCount > 0),
  );
  readonly barHeight = computed(() =>
    Math.max(280, this.people().value.length * 36),
  );

  constructor() {
    const api = inject(MetricsApiService);
    const destroyRef = inject(DestroyRef);
    this.refresh
      .pipe(
        switchMap(() => requestState(api.collaboratorCounts(), [])),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((view) => this.people.set(view));
    this.refresh
      .pipe(
        switchMap(() => requestState(api.workshopCounts(), [])),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((view) => this.workshops.set(view));
  }

  /** Reloads both metrics from the server; e.g. after a participation was removed. */
  retry(): void {
    this.refresh.next(this.refresh.value + 1);
  }
}

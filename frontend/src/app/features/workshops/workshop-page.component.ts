import { Component, DestroyRef, inject, signal } from "@angular/core";
import { DatePipe } from "@angular/common";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { BehaviorSubject, combineLatest, finalize, switchMap } from "rxjs";
import { WorkshopsApiService } from "../../core/api/workshops-api.service";
import { AttendanceRecord } from "../../core/models/workshop.models";
import { requestState, RequestState } from "../../core/api/request-state";
import { LoadingStateComponent } from "../../shared/loading-state/loading-state.component";
import { EmptyStateComponent } from "../../shared/empty-state/empty-state.component";
import { ErrorStateComponent } from "../../shared/error-state/error-state.component";

@Component({
  selector: "app-workshop-page",
  imports: [
    DatePipe,
    RouterLink,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: "./workshop-page.component.html",
  styleUrl: "./workshop-page.component.scss",
})
export class WorkshopPageComponent {
  readonly view = signal<RequestState<AttendanceRecord | null>>({
    value: null,
    loading: true,
    error: "",
  });
  private readonly refresh = new BehaviorSubject<number>(0);
  readonly removingId = signal<number | null>(null);
  readonly removalError = signal("");
  readonly removalNotice = signal("");
  private readonly api: WorkshopsApiService;
  private readonly destroyRef: DestroyRef;

  constructor() {
    this.api = inject(WorkshopsApiService);
    this.destroyRef = inject(DestroyRef);
    this.observeWorkshop(this.api, inject(ActivatedRoute), this.destroyRef);
  }

  private observeWorkshop(
    api: WorkshopsApiService,
    route: ActivatedRoute,
    destroyRef: DestroyRef,
  ): void {
    combineLatest([route.paramMap, this.refresh])
      .pipe(
        switchMap(([params]) =>
          requestState<AttendanceRecord | null>(
            api.workshopAttendance(Number(params.get("id"))),
            null,
          ),
        ),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((view) => this.view.set(view));
  }

  /** Reloads details after a transient failure; e.g. the API is restarted. */
  retry(): void {
    this.refresh.next(this.refresh.value + 1);
  }

  /** Removes participation after API confirmation; e.g. person 2 from the displayed attendance. */
  removeParticipant(collaboratorId: number): void {
    const attendance = this.view().value;
    if (!attendance || this.removingId() !== null) return;
    this.removingId.set(collaboratorId);
    this.removalError.set("");
    this.removalNotice.set("");
    this.sendRemoval(attendance.id, collaboratorId);
  }

  private sendRemoval(attendanceId: number, collaboratorId: number): void {
    this.api
      .removeParticipant(attendanceId, collaboratorId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.removingId.set(null)),
      )
      .subscribe({
        next: () => this.applyRemoval(attendanceId, collaboratorId),
        error: () =>
          this.removalError.set(
            "Não foi possível remover a participação. Tente novamente.",
          ),
      });
  }

  private applyRemoval(attendanceId: number, collaboratorId: number): void {
    const current = this.view();
    const attendance = current.value;
    if (!attendance || attendance.id !== attendanceId) return;
    const collaborators = attendance.collaborators.filter(
      (person) => person.id !== collaboratorId,
    );
    this.view.set({ ...current, value: { ...attendance, collaborators } });
    this.removalNotice.set("Participação removida da ata.");
  }
}

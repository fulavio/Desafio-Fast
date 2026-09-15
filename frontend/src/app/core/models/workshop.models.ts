export interface Workshop {
  readonly id: number;
  readonly name: string;
  readonly heldAt: string;
  readonly description: string;
}
export interface Collaborator {
  readonly id: number;
  readonly name: string;
}
export interface AttendanceRecord {
  readonly id: number;
  readonly workshop: Workshop;
  readonly collaborators: readonly Collaborator[];
}
export interface AttendanceFilters {
  readonly workshopNome: string;
  readonly data: string;
  readonly colaborador: string;
}

export interface AttendanceSummary extends AttendanceRecord {
  readonly participantCount: number;
}

export interface AttendancePage {
  readonly items: readonly AttendanceSummary[];
  readonly total: number;
}

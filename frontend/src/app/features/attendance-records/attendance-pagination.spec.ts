import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { provideRouter } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { vi } from "vitest";
import { routes } from "../../app.routes";
import { AttendancePageComponent } from "./attendance-page.component";
import {
  FakeWorkshopServer,
  sampleAttendance,
} from "../../../testing/fake-workshop-server";
import { FakeIntersectionObserver } from "../../../testing/fake-intersection-observer";
import { AttendanceRecord } from "../../core/models/workshop.models";

const manyRecords: readonly AttendanceRecord[] = Array.from(
  { length: 14 },
  (_, index) => ({
    ...sampleAttendance[0],
    id: index + 1,
    workshop: {
      ...sampleAttendance[0].workshop,
      id: index + 1,
      name: `Workshop ${index + 1}`,
    },
    collaborators: Array.from({ length: 24 }, (_, person) => ({
      id: person + 1,
      name: `Pessoa ${person + 1}`,
    })),
  }),
);

describe("Attendance pagination", () => {
  let server: FakeWorkshopServer;
  let http: HttpTestingController;

  beforeEach(() => {
    FakeIntersectionObserver.instances = [];
    vi.stubGlobal("IntersectionObserver", FakeIntersectionObserver);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(routes),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    server = new FakeWorkshopServer(http);
  });
  afterEach(() => {
    http.verify();
    vi.unstubAllGlobals();
  });

  it("loads six at a time on scroll, ignores duplicate triggers and stops at the last page", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    server.replyAttendancePage(manyRecords.slice(0, 6), "", "", "", 1, 14);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll("article"),
    ).toHaveLength(6);
    const observer = FakeIntersectionObserver.instances[0];
    observer.enter(false);
    http.expectNone((request) => request.url.endsWith("/atas/pagina"));
    observer.enter();
    observer.enter();
    server.replyAttendancePage(manyRecords.slice(6, 12), "", "", "", 2, 14);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll("article"),
    ).toHaveLength(12);
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>("[appLoadMore]")!
      .click();
    server.replyAttendancePage(manyRecords.slice(12), "", "", "", 3, 14);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll("article"),
    ).toHaveLength(14);
    expect(harness.routeNativeElement!.textContent).toContain(
      "14 de 14 encontros exibidos",
    );
    expect(
      harness.routeNativeElement!.querySelector("[appLoadMore]"),
    ).toBeNull();
    expect(observer.disconnected).toBe(true);
  });

  it("keeps loaded cards on failure and retries the same page", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    server.replyAttendancePage(manyRecords.slice(0, 6), "", "", "", 1, 14);
    await harness.fixture.whenStable();
    FakeIntersectionObserver.instances[0].enter();
    server.fail("/atas/pagina");
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll("article"),
    ).toHaveLength(6);
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>(
        "app-error-state button",
      )!
      .click();
    server.replyAttendancePage(manyRecords.slice(6, 12), "", "", "", 2, 14);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll("article"),
    ).toHaveLength(12);
    expect(
      harness.routeNativeElement!.querySelector("app-error-state"),
    ).toBeNull();
  });

  it("cancels an outstanding next page when filters change and starts from page one", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    server.replyAttendancePage(manyRecords.slice(0, 6), "", "", "", 1, 14);
    await harness.fixture.whenStable();
    FakeIntersectionObserver.instances[0].enter();
    await harness.navigateByUrl(
      "/atas?colaborador=Pessoa%2024",
      AttendancePageComponent,
    );
    server.expectCancelledAttendance("/atas/pagina?pagina=2&tamanhoPagina=6");
    server.replyAttendancePage([manyRecords[13]], "", "", "Pessoa 24", 1, 1);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll("article"),
    ).toHaveLength(1);
    expect(harness.routeNativeElement!.textContent).toContain("Workshop 14");
    expect(
      harness.routeNativeElement!.querySelector("h3 a")!.getAttribute("href"),
    ).toContain("colaborador=Pessoa%2024");
  });

  it("limits card previews to seven names while preserving the total and complete details", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    server.replyAttendancePage([manyRecords[0]]);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll(".participant-preview li"),
    ).toHaveLength(7);
    expect(harness.routeNativeElement!.textContent).toContain(
      "24 participantes",
    );
    harness
      .routeNativeElement!.querySelector<HTMLAnchorElement>(".detail-link")!
      .click();
    await harness.fixture.whenStable();
    server.replyDetails(manyRecords[0]);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll(".participant-list li"),
    ).toHaveLength(24);
  });

  it("keeps manual loading available without IntersectionObserver", async () => {
    vi.stubGlobal("IntersectionObserver", undefined);
    const harness = await RouterTestingHarness.create("/atas");
    server.replyAttendancePage(manyRecords.slice(0, 6), "", "", "", 1, 7);
    await harness.fixture.whenStable();
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>("[appLoadMore]")!
      .click();
    server.replyAttendancePage([manyRecords[6]], "", "", "", 2, 7);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelectorAll("article"),
    ).toHaveLength(7);
  });
});

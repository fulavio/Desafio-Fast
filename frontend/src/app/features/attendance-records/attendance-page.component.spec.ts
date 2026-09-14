import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { routes } from "../../app.routes";
import { AttendancePageComponent } from "./attendance-page.component";
import {
  FakeWorkshopServer,
  sampleAttendance,
} from "../../../testing/fake-workshop-server";

describe("Attendance page", () => {
  let server: FakeWorkshopServer;
  let http: HttpTestingController;

  beforeEach(() => {
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
  afterEach(() => http.verify());

  it("renders loading, participants, calendar dates and real detail links", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    expect(harness.routeNativeElement?.textContent).toContain("Carregando");
    server.replyAttendance();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.querySelectorAll("article").length).toBe(
      2,
    );
    expect(harness.routeNativeElement?.textContent).toContain("Ana Souza");
    expect(harness.routeNativeElement?.textContent).toContain("09/07/2026");
    expect(
      harness.routeNativeElement?.querySelector("h3 a")?.getAttribute("href"),
    ).toBe("/workshops/1");
  });

  it("restores query filters and only filters collaborator names in the client", async () => {
    const harness = await RouterTestingHarness.create(
      "/atas?workshopNome=Code&data=2026-07-09&colaborador=ana",
    );
    server.replyAttendance(sampleAttendance, "Code", "2026-07-09");
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.querySelectorAll("article").length).toBe(
      1,
    );
    expect(
      harness.routeNativeElement?.querySelector<HTMLInputElement>(
        "#collaborator-name",
      )?.value,
    ).toBe("ana");
  });

  it("submits and clears filters through the visible form", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    server.replyAttendance();
    await harness.fixture.whenStable();
    const input =
      harness.routeNativeElement!.querySelector<HTMLInputElement>(
        "#workshop-name",
      )!;
    input.value = " Code ";
    input.dispatchEvent(new Event("input"));
    harness
      .routeNativeElement!.querySelector("form")!
      .dispatchEvent(new Event("submit"));
    await harness.fixture.whenStable();
    server.replyAttendance([], "Code");
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe("/atas?workshopNome=Code");
    expect(harness.routeNativeElement?.textContent).toContain(
      "Nenhuma ata encontrada",
    );
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>(".text-button")!
      .click();
    await harness.fixture.whenStable();
    server.replyAttendance();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe("/atas");
    expect(harness.routeNativeElement?.querySelectorAll("article").length).toBe(
      2,
    );
  });

  it("retries an error without needing to change the URL", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    server.fail("/atas");
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement?.querySelector('[role="alert"]'),
    ).not.toBeNull();
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>(
        "app-error-state button",
      )!
      .click();
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain("Carregando");
    server.replyAttendance();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.querySelectorAll("article").length).toBe(
      2,
    );
  });

  it("cancels previous requests when filters change", async () => {
    const harness = await RouterTestingHarness.create("/atas");
    await harness.navigateByUrl(
      "/atas?workshopNome=Angular",
      AttendancePageComponent,
    );
    server.expectCancelledAttendance();
    server.replyAttendance([sampleAttendance[1]], "Angular");
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.textContent).toContain("Angular");
    expect(harness.routeNativeElement?.textContent).not.toContain("Clean Code");
  });

  it("shows empty state when the client filter has no match", async () => {
    const harness = await RouterTestingHarness.create(
      "/atas?colaborador=Nobody",
    );
    server.replyAttendance();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.textContent).toContain(
      "Nenhuma ata encontrada",
    );
    expect(harness.routeNativeElement?.querySelectorAll("article").length).toBe(
      0,
    );
  });

  it.each(["/", "/unknown"])(
    "redirects %s to attendance",
    async (path: string) => {
      const harness = await RouterTestingHarness.create(path);
      server.replyAttendance([]);
      await harness.fixture.whenStable();
      expect(TestBed.inject(Router).url).toBe("/atas");
    },
  );
});

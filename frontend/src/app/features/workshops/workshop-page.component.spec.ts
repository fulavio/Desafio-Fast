import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { provideRouter } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { routes } from "../../app.routes";
import {
  FakeWorkshopServer,
  sampleAttendance,
} from "../../../testing/fake-workshop-server";

describe("Workshop details", () => {
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

  it("renders details, participants and a return link preserving filters", async () => {
    const harness = await RouterTestingHarness.create(
      "/workshops/1?colaborador=Ana",
    );
    expect(harness.routeNativeElement?.textContent).toContain("Carregando");
    server.replyDetails();
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement?.querySelector("h1")?.textContent,
    ).toContain("Clean Code");
    expect(harness.routeNativeElement?.textContent).toContain(
      "Código legível.",
    );
    expect(
      harness.routeNativeElement?.querySelectorAll(".participant-list li")
        .length,
    ).toBe(2);
    expect(
      harness.routeNativeElement
        ?.querySelector(".back-link")
        ?.getAttribute("href"),
    ).toBe("/atas?colaborador=Ana");
  });

  it("shows an empty participant state", async () => {
    const harness = await RouterTestingHarness.create("/workshops/1");
    server.replyDetails({
      id: 8,
      workshop: sampleAttendance[0].workshop,
      collaborators: [],
    });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.textContent).toContain(
      "Nenhum participante registrado",
    );
  });

  it("shows attendance request errors and supports retry", async () => {
    const harness = await RouterTestingHarness.create("/workshops/1");
    server.fail("/atas", 503);
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.textContent).toContain(
      "Não foi possível carregar",
    );
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>(
        "app-error-state button",
      )!
      .click();
    server.replyDetails();
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement?.querySelector("h1")?.textContent,
    ).toContain("Clean Code");
  });

  it("finds the workshop by its ID even when it is not the first attendance", async () => {
    const harness = await RouterTestingHarness.create(
      "/workshops/2?workshopNome=Code",
    );
    server.replyAttendance();
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement?.querySelector("h1")?.textContent,
    ).toContain("Angular");
    expect(harness.routeNativeElement?.textContent).not.toContain("Clean Code");
  });

  it.each(["/workshops/99", "/workshops/invalid"])(
    "shows missing attendance for %s",
    async (path: string) => {
      const harness = await RouterTestingHarness.create(path);
      server.replyAttendance();
      await harness.fixture.whenStable();
      expect(harness.routeNativeElement?.textContent).toContain(
        "Ata não encontrada",
      );
      expect(
        harness.routeNativeElement?.querySelector(".participant-list"),
      ).toBeNull();
    },
  );
});

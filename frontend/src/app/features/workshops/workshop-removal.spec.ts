import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { provideRouter } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { routes } from "../../app.routes";
import { WorkshopPageComponent } from "./workshop-page.component";
import {
  FakeWorkshopServer,
  sampleAttendance,
} from "../../../testing/fake-workshop-server";

describe("Remove attendance participants", () => {
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

  it("uses the attendance ID, blocks duplicate clicks and updates only after success", async () => {
    const harness = await RouterTestingHarness.create("/workshops/1");
    server.replyDetails({ ...sampleAttendance[0], id: 8 });
    await harness.fixture.whenStable();
    const page = harness.routeDebugElement!
      .componentInstance as WorkshopPageComponent;
    clickRemoval(harness);
    page.removeParticipant(1);
    harness.detectChanges();
    expect(participantCount(harness)).toBe(2);
    expect(
      harness.routeNativeElement?.querySelector<HTMLButtonElement>("button")
        ?.disabled,
    ).toBe(true);
    server.replyRemoval(8, 1);
    await harness.fixture.whenStable();
    expect(participantCount(harness)).toBe(1);
    expect(harness.routeNativeElement?.textContent).not.toContain("Ana Souza");
    expect(harness.routeNativeElement?.textContent).toContain("Bruno Lima");
    expect(harness.routeNativeElement?.textContent).toContain(
      "Participação removida",
    );
  });

  it("shows empty state after removing the last participant", async () => {
    const harness = await RouterTestingHarness.create("/workshops/2");
    server.replyDetails(sampleAttendance[1]);
    await harness.fixture.whenStable();
    clickRemoval(harness);
    server.replyRemoval(2, 2);
    await harness.fixture.whenStable();
    expect(participantCount(harness)).toBe(0);
    expect(harness.routeNativeElement?.textContent).toContain(
      "Nenhum participante registrado",
    );
    expect(
      harness.routeNativeElement?.querySelector("h1")?.textContent,
    ).toContain("Angular");
  });

  it("preserves participants after a failed DELETE and allows retry", async () => {
    const harness = await RouterTestingHarness.create("/workshops/1");
    server.replyDetails();
    await harness.fixture.whenStable();
    clickRemoval(harness);
    server.fail("/atas/1/colaboradores/1");
    await harness.fixture.whenStable();
    expect(participantCount(harness)).toBe(2);
    expect(
      harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent,
    ).toContain("Não foi possível remover");
    clickRemoval(harness);
    server.replyRemoval(1, 1);
    await harness.fixture.whenStable();
    expect(participantCount(harness)).toBe(1);
    expect(
      harness.routeNativeElement?.querySelector('[role="alert"]'),
    ).toBeNull();
  });
});

function clickRemoval(harness: RouterTestingHarness): void {
  harness
    .routeNativeElement!.querySelector<HTMLButtonElement>(
      ".remove-participant",
    )!
    .click();
}

function participantCount(harness: RouterTestingHarness): number {
  return harness.routeNativeElement!.querySelectorAll(".participant-list li")
    .length;
}

import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { provideRouter } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { BaseChartDirective } from "ng2-charts";
import { ChartData, LegendItem } from "chart.js";
import { routes } from "../../app.routes";
import { MetricsPageComponent } from "./metrics-page.component";
import { FakeChartDirective } from "../../../testing/fake-chart.directive";
import { FakeMetricsServer } from "../../../testing/fake-metrics-server";

describe("Metrics page", () => {
  let http: HttpTestingController;
  let server: FakeMetricsServer;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(routes),
      ],
    });
    TestBed.overrideComponent(MetricsPageComponent, {
      remove: { imports: [BaseChartDirective] },
      add: { imports: [FakeChartDirective] },
    });
    http = TestBed.inject(HttpTestingController);
    server = new FakeMetricsServer(http);
  });
  afterEach(() => http.verify());

  it("loads both charts with exactly two aggregate requests", async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl("/metricas", MetricsPageComponent);
    expect(harness.routeNativeElement?.textContent).toContain("Carregando");
    server.people();
    server.workshops();
    await harness.fixture.whenStable();
    expect(page.barChart().datasets[0].data).toEqual([17, 0]);
    expect(page.barChart().labels).toEqual(["Ana", "Ana"]);
    expect(page.pieChart().datasets[0].data).toEqual([8, 0]);
    expect(page.pieChart().labels).toEqual(["Angular", "Code"]);
    const rowNames = Array.from(
      harness.routeNativeElement!.querySelectorAll("tbody th"),
      (cell) => cell.textContent?.trim(),
    );
    expect(rowNames).toEqual(["Ana", "Ana", "Angular", "Code"]);
    expect(page.barHeight()).toBe(280);
    expect(harness.routeNativeElement?.querySelectorAll("canvas").length).toBe(
      2,
    );
    expect(
      harness.routeNativeElement?.querySelectorAll("tbody tr").length,
    ).toBe(4);
    expect(harness.routeNativeElement?.textContent).toContain("17");
    expect(
      harness.routeNativeElement
        ?.querySelector(".back-link")
        ?.getAttribute("href"),
    ).toBe("/atas");
  });

  it("sorts pie legends by descending totals while preserving slice indices and colors", async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl("/metricas", MetricsPageComponent);
    server.people();
    server.workshops();
    const chart: ChartData<"pie"> = {
      labels: ["Code", "Angular", "Empty", "Testing"],
      datasets: [{ data: [1, 8, 0, 8] }],
    };
    const legends: LegendItem[] = chart.labels!.map((label, index) => ({
      text: String(label),
      index,
      fillStyle: ["red", "blue", "gray", "green"][index],
    }));
    const sort = page.pieOptions.plugins!.legend!.labels!.sort!;
    const ordered = [...legends].sort((first, second) =>
      sort(first, second, chart),
    );
    expect(ordered).toEqual([legends[1], legends[3], legends[0], legends[2]]);
    expect(chart.datasets[0].data).toEqual([1, 8, 0, 8]);
    expect(chart.labels).toEqual(["Code", "Angular", "Empty", "Testing"]);
  });

  it("handles empty lists without waiting for nonexistent per-person requests", async () => {
    const harness = await RouterTestingHarness.create("/metricas");
    server.people(true, true);
    server.workshops(true, true);
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.textContent).toContain(
      "Nenhum colaborador cadastrado",
    );
    expect(harness.routeNativeElement?.textContent).toContain(
      "Nenhum workshop cadastrado",
    );
    expect(harness.routeNativeElement?.querySelector("canvas")).toBeNull();
  });

  it("shows zero totals without drawing an empty pie", async () => {
    const harness = await RouterTestingHarness.create("/metricas");
    server.people(true);
    server.workshops(true);
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.textContent).toContain(
      "Nenhuma participação registrada",
    );
    expect(harness.routeNativeElement?.querySelectorAll("canvas").length).toBe(
      1,
    );
    expect(
      harness.routeNativeElement?.querySelectorAll("tbody tr").length,
    ).toBe(4);
  });

  it("keeps workshop metrics visible if a person request fails and retries", async () => {
    const harness = await RouterTestingHarness.create("/metricas");
    server.fail("/metrics/colaboradores/workshops-count");
    server.workshops();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.textContent).toContain(
      "Não foi possível carregar",
    );
    expect(harness.routeNativeElement?.textContent).toContain("Angular");
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>(
        "app-error-state button",
      )!
      .click();
    server.people();
    server.workshops();
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement?.querySelector("app-error-state"),
    ).toBeNull();
  });

  it("reports errors for both aggregate endpoints", async () => {
    const harness = await RouterTestingHarness.create("/metricas");
    server.fail("/metrics/colaboradores/workshops-count");
    server.fail("/metrics/workshops/colaboradores-count");
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement?.querySelectorAll("app-error-state").length,
    ).toBe(2);
  });
});

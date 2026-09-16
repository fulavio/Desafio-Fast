import { Directive, input } from "@angular/core";
import { ChartData, ChartOptions } from "chart.js";

/** Replaces canvas rendering in jsdom while preserving chart inputs; e.g. bar dataset assertions. */
@Directive({ selector: "canvas[baseChart]" })
export class FakeChartDirective {
  readonly type = input<"bar" | "pie">("bar");
  readonly data = input<ChartData>();
  readonly options = input<ChartOptions>();
}

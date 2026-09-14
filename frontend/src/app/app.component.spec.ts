import { TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { AppComponent } from "./app.component";

describe("Application shell", () => {
  it("focuses main without navigating away from the active page", async () => {
    TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(AppComponent);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const click = new MouseEvent("click", { bubbles: true, cancelable: true });
    element
      .querySelector<HTMLAnchorElement>(".skip-link")!
      .dispatchEvent(click);
    expect(click.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(element.querySelector("main"));
  });
  it("provides a skip link, main landmark, brand navigation and footer", async () => {
    TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(AppComponent);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector(".skip-link")?.getAttribute("href")).toBe(
      "#main",
    );
    expect(element.querySelector("main")?.id).toBe("main");
    expect(element.querySelector(".brand")?.getAttribute("href")).toBe("/atas");
    expect(element.querySelector("footer")?.textContent).toContain(
      "FAST Soluções",
    );
  });
});

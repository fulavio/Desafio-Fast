import { Component } from "@angular/core";
import { RouterLink, RouterOutlet } from "@angular/router";

@Component({
  selector: "app-root",
  imports: [RouterLink, RouterOutlet],
  template: `
    <a class="skip-link" href="#main" (click)="focusMain($event, mainContent)"
      >Ir para o conteúdo</a
    >
    <header class="site-header">
      <a class="brand" routerLink="/atas" aria-label="FAST Workshops, início"
        >FAST<span>soluções</span></a
      >
      <span class="header-divider"></span
      ><span class="product-name">Workshops</span>
      <span class="internal-label">DESENVOLVIMENTO & CONEXÃO</span>
    </header>
    <main #mainContent id="main" tabindex="-1"><router-outlet /></main>
    <footer>
      FAST Soluções <span>Aprendemos juntos. Evoluímos juntos.</span>
    </footer>
  `,
})
export class AppComponent {
  /** Moves keyboard focus without losing the current route; e.g. workshop details stay open. */
  focusMain(event: MouseEvent, content: HTMLElement): void {
    event.preventDefault();
    content.focus();
  }
}

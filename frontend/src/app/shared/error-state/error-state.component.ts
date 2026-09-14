import { Component, input, output } from "@angular/core";

@Component({
  selector: "app-error-state",
  template:
    '<div class="state-panel error-panel" role="alert"><h2>Não foi possível carregar</h2><p>{{ message() }}</p><button type="button" (click)="retry.emit()">Tentar novamente</button></div>',
})
export class ErrorStateComponent {
  readonly message = input.required<string>();
  readonly retry = output<void>();
}

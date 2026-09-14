import { Component } from "@angular/core";

@Component({
  selector: "app-loading-state",
  template:
    '<div class="state-panel" role="status"><span class="loading-dot"></span>Carregando workshops…</div>',
})
export class LoadingStateComponent {}

import { Component, input } from "@angular/core";

@Component({
  selector: "app-empty-state",
  template:
    '<div class="state-panel" role="status"><h2>{{ title() }}</h2><p>{{ description() }}</p></div>',
})
export class EmptyStateComponent {
  readonly title = input("Nenhuma ata encontrada");
  readonly description = input(
    "Experimente outros filtros ou limpe a busca para ver todas as atas.",
  );
}

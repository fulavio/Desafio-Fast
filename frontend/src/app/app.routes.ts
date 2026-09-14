import { Routes } from "@angular/router";

export const routes: Routes = [
  {
    path: "atas",
    loadComponent: () =>
      import("./features/attendance-records/attendance-page.component").then(
        (module) => module.AttendancePageComponent,
      ),
  },
  {
    path: "workshops/:id",
    loadComponent: () =>
      import("./features/workshops/workshop-page.component").then(
        (module) => module.WorkshopPageComponent,
      ),
  },
  { path: "", pathMatch: "full", redirectTo: "atas" },
  { path: "**", redirectTo: "atas" },
];

import {
  afterNextRender,
  DestroyRef,
  Directive,
  ElementRef,
  inject,
  output,
} from "@angular/core";

/** Observes the end of the list; e.g. entering the viewport requests another page. */
@Directive({ selector: "[appLoadMore]" })
export class LoadMoreDirective {
  readonly reached = output<void>();

  constructor() {
    const element = inject<ElementRef<HTMLElement>>(ElementRef);
    const destroyRef = inject(DestroyRef);
    afterNextRender(() => {
      if (typeof IntersectionObserver === "undefined") return;
      const observer = new IntersectionObserver((entries) => {
        if (entries.some((entry) => entry.isIntersecting)) this.reached.emit();
      });
      observer.observe(element.nativeElement);
      destroyRef.onDestroy(() => observer.disconnect());
    });
  }
}

/** Controls viewport notifications without layout; e.g. enter() simulates reaching the list end. */
export class FakeIntersectionObserver {
  static instances: FakeIntersectionObserver[] = [];
  private target: Element | null = null;
  disconnected = false;

  constructor(private readonly callback: IntersectionObserverCallback) {
    FakeIntersectionObserver.instances.push(this);
  }

  /** Tracks the sentinel; e.g. the load-more button. */
  observe(target: Element): void {
    this.target = target;
  }

  /** Simulates visibility; e.g. false must not request another page. */
  enter(isIntersecting = true): void {
    if (!this.target || this.disconnected) return;
    const entry = {
      isIntersecting,
      target: this.target,
    } as IntersectionObserverEntry;
    this.callback([entry], this as unknown as IntersectionObserver);
  }

  /** Releases the target; e.g. navigating to details destroys the observer. */
  disconnect(): void {
    this.disconnected = true;
  }
}

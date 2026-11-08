import { DestroyRef, ElementRef, Signal, inject, signal } from '@angular/core';

export function hostWidth(): Signal<number> {
  const host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  const width = signal(host.clientWidth);
  const observer = new ResizeObserver(([entry]) => width.set(entry.contentRect.width));

  observer.observe(host);
  inject(DestroyRef).onDestroy(() => observer.disconnect());

  return width.asReadonly();
}

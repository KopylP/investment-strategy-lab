import { Select } from 'primeng/select';

export function fitNodeSelectOverlay(select: Select): void {
  requestAnimationFrame(() => {
    const overlay = select.overlayViewChild?.overlayEl;
    const target = select.el.nativeElement as HTMLElement;
    if (!overlay || !target) return;
    // PrimeNG uses an unscaled CSS width; nodes are transformed on the canvas.
    const width = Math.min(target.getBoundingClientRect().width, window.innerWidth - 24);
    overlay.style.width = `${width}px`;
    overlay.style.minWidth = `${width}px`;
    overlay.style.maxWidth = `${width}px`;
  });
}

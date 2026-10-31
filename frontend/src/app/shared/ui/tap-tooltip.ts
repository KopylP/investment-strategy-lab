import { AfterContentInit, Directive, ElementRef, inject, OnDestroy } from '@angular/core';
import { Tooltip } from 'primeng/tooltip';

const touchScreen = window.matchMedia('(hover: none)').matches;
const edge = 8;

@Directive({
  selector: '[pTooltip]',
  host: { '[class.app-tap-tooltip]': 'tappable', '(click)': 'toggle()' },
})
export class TapTooltip implements AfterContentInit, OnDestroy {
  private readonly tooltip = inject(Tooltip, { self: true });
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private stopListening: (() => void) | null = null;

  protected tappable = false;

  ngAfterContentInit(): void {
    if (!touchScreen) return;
    const interactive =
      !!this.host.closest('button, a, p-button') ||
      getComputedStyle(this.host).cursor === 'pointer';
    this.tappable = !interactive;
    this.tooltip.setOption(
      interactive ? { tooltipEvent: 'none', disabled: true } : { tooltipEvent: 'none' },
    );
  }

  protected toggle(): void {
    if (!this.tappable) return;
    if (this.tooltip.container) {
      this.close();
      return;
    }
    this.tooltip.activate();
    this.place();
    const outside = (event: Event) => {
      const target = event.target as Node;
      if (!this.host.contains(target) && !this.tooltip.container?.contains(target)) this.close();
    };
    document.addEventListener('pointerdown', outside, true);
    this.stopListening = () => document.removeEventListener('pointerdown', outside, true);
  }

  private close(): void {
    this.tooltip.deactivate();
    this.stopListening?.();
    this.stopListening = null;
  }

  private place(): void {
    const container = this.tooltip.container as HTMLElement | undefined;
    if (!container) return;
    container.style.left = '0px';
    container.style.width = `${container.getBoundingClientRect().width}px`;
    this.tooltip.alignTop();
    if (container.getBoundingClientRect().top < edge) this.tooltip.alignBottom();

    const box = container.getBoundingClientRect();
    const shift =
      Math.max(edge - box.left, 0) -
      Math.max(box.right - (document.documentElement.clientWidth - edge), 0);
    if (!shift) return;
    container.style.left = `${parseFloat(container.style.left) + shift}px`;
    const arrow = container.querySelector<HTMLElement>('[data-pc-section="arrow"]');
    if (arrow) arrow.style.left = `${parseFloat(arrow.style.left) - shift}px`;
  }

  ngOnDestroy(): void {
    this.stopListening?.();
  }
}

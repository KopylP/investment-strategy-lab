import { AfterContentInit, Directive, ElementRef, inject, OnDestroy } from '@angular/core';
import { Tooltip } from 'primeng/tooltip';

/** A finger has no hover: on such screens a tooltip is a tap away. */
const touchScreen = window.matchMedia('(hover: none)').matches;
/** Space kept between a tooltip and the screen's edge. */
const edge = 8;

/**
 * pTooltip on a touch screen: PrimeNG shows it only while the finger stays down (and iOS selects the text meanwhile),
 * and "right" of an icon by the edge squeezes it to a letter per line. Here a tap opens it above the element (below
 * when there is no room), kept inside the screen, and a tap anywhere else closes it. Tooltips on what a tap already
 * does something with (buttons, links, clickable rows) stay off there. Import it wherever Tooltip is imported.
 */
@Directive({
  selector: '[pTooltip]',
  host: { '[class.app-tap-tooltip]': 'tappable', '(click)': 'toggle()' },
})
export class TapTooltip implements AfterContentInit, OnDestroy {
  private readonly tooltip = inject(Tooltip, { self: true });
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private stopListening: (() => void) | null = null;

  protected tappable = false;

  // Before PrimeNG binds its own listeners (after the view): none of them, on a touch screen.
  ngAfterContentInit(): void {
    if (!touchScreen) return;
    const interactive = !!this.host.closest('button, a, p-button') || getComputedStyle(this.host).cursor === 'pointer';
    this.tappable = !interactive;
    this.tooltip.setOption(interactive ? { tooltipEvent: 'none', disabled: true } : { tooltipEvent: 'none' });
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

  /** Above (or below), then shifted sideways into the screen with the arrow still pointing at the element. */
  private place(): void {
    const container = this.tooltip.container as HTMLElement | undefined;
    if (!container) return;
    // Its own width, measured at the left edge: placed by the right edge the browser would squeeze it instead of
    // letting it overflow, and there would be nothing to shift back.
    container.style.left = '0px';
    container.style.width = `${container.getBoundingClientRect().width}px`;
    this.tooltip.alignTop();
    if (container.getBoundingClientRect().top < edge) this.tooltip.alignBottom();

    const box = container.getBoundingClientRect();
    const shift = Math.max(edge - box.left, 0) - Math.max(box.right - (document.documentElement.clientWidth - edge), 0);
    if (!shift) return;
    container.style.left = `${parseFloat(container.style.left) + shift}px`;
    const arrow = container.querySelector<HTMLElement>('[data-pc-section="arrow"]');
    if (arrow) arrow.style.left = `${parseFloat(arrow.style.left) - shift}px`;
  }

  ngOnDestroy(): void {
    this.stopListening?.();
  }
}

import { Injectable, signal } from '@angular/core';

/** Phone width (below Tailwind's `sm`, 576px): views drop secondary columns, labels and chart detail. */
@Injectable({ providedIn: 'root' })
export class Viewport {
  private readonly query = window.matchMedia('(max-width: 575.98px)');

  readonly phone = signal(this.query.matches);

  constructor() {
    this.query.addEventListener('change', (event) => this.phone.set(event.matches));
  }
}

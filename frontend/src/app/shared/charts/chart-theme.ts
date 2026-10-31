import { computed, Injectable, signal } from '@angular/core';

export interface ChartPalette {
  text: string;
  muted: string;
  grid: string;
  primary: string;
  primarySoft: string;
  gain: string;
  loss: string;
  neutral: string;
  tax: string;
  series: string[];
}

@Injectable({ providedIn: 'root' })
export class ChartTheme {
  private readonly themeVersion = signal(0);

  constructor() {
    new MutationObserver(() => this.themeVersion.update((version) => version + 1)).observe(
      document.documentElement,
      { attributes: true, attributeFilter: ['class'] },
    );
  }

  readonly palette = computed<ChartPalette>(() => {
    this.themeVersion();
    const style = getComputedStyle(document.documentElement);
    const token = (name: string, fallback: string) =>
      style.getPropertyValue(name).trim() || fallback;
    const primary = token('--p-primary-500', '#6366f1');

    return {
      text: token('--p-text-color', '#334155'),
      muted: token('--p-text-muted-color', '#64748b'),
      grid: token('--p-content-border-color', '#e2e8f0'),
      primary,
      primarySoft: token('--p-primary-200', '#c7d2fe'),
      gain: token('--p-finance-gain', '#16a34a'),
      loss: token('--p-finance-loss', '#dc2626'),
      neutral: token('--p-finance-neutral', '#64748b'),
      tax: token('--p-finance-tax', '#d97706'),
      series: [
        primary,
        token('--p-amber-500', '#f59e0b'),
        token('--p-teal-500', '#14b8a6'),
        token('--p-pink-500', '#ec4899'),
        token('--p-sky-500', '#0ea5e9'),
      ],
    };
  });
}

export function withAlpha(color: string, alpha: number): string {
  const hex = Math.round(Math.min(1, Math.max(0, alpha)) * 255)
    .toString(16)
    .padStart(2, '0');

  return /^#[0-9a-f]{6}$/i.test(color) ? `${color}${hex}` : color;
}

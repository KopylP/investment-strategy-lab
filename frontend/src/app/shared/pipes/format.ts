const locale = 'uk-UA';

export function formatMoney(amount: number | null | undefined, currency: string, digits = 2): string {
  if (amount === null || amount === undefined) return '—';
  return new Intl.NumberFormat(locale, { style: 'currency', currency, minimumFractionDigits: digits, maximumFractionDigits: digits }).format(amount);
}

/**
 * A quantity exactly as stored: the API keeps up to 8 decimals (fractional shares), so all 8 are shown — fewer would
 * round, and a rounded-up quantity is more than the plan or the holding has.
 */
export function formatQuantity(quantity: number | null | undefined): string {
  if (quantity === null || quantity === undefined) return '—';
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 8 }).format(quantity);
}

export function formatPercent(fraction: number | null | undefined, digits = 1): string {
  if (fraction === null || fraction === undefined) return '—';
  return new Intl.NumberFormat(locale, { style: 'percent', minimumFractionDigits: digits, maximumFractionDigits: digits }).format(fraction);
}

export function formatSignedPercent(fraction: number | null | undefined, digits = 1): string {
  if (fraction === null || fraction === undefined) return '—';
  return new Intl.NumberFormat(locale, { style: 'percent', minimumFractionDigits: digits, maximumFractionDigits: digits, signDisplay: 'exceptZero' }).format(fraction);
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';
  const date = value.length === 10 ? new Date(`${value}T00:00:00`) : new Date(value);
  return new Intl.DateTimeFormat(locale, { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—';
  return new Intl.DateTimeFormat(locale, { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
}

export function formatPeriod(period: string): string {
  const [year, month] = period.split('-').map(Number);
  const name = new Intl.DateTimeFormat(locale, { month: 'long' }).format(new Date(year, month - 1, 1));
  return `${name} ${year}`;
}

/** "26.04.2026" → "26.04.26": a chart axis label on a phone. */
export function shortenDate(label: string): string {
  return /^\d{2}\.\d{2}\.\d{4}$/.test(label) ? `${label.slice(0, 6)}${label.slice(8)}` : label;
}

/**
 * An axis number without the currency ("1 150", "125 тис.") — the currency is in the figure above the chart. Short
 * scale only from 100 000: below it neighbouring ticks would round to the same label.
 */
export function formatAxisNumber(value: number, digits = 0): string {
  return Math.abs(value) >= 100_000
    ? new Intl.NumberFormat(locale, { notation: 'compact', maximumFractionDigits: 1 }).format(value)
    : new Intl.NumberFormat(locale, { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(value);
}

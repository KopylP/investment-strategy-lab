import english from '../../../../../locales/en.json';
import ukrainian from '../../../../../locales/uk.json';
import { activeLanguage, activeLocale, Language } from './language';

type Parameters = Record<string, unknown>;

const tokenPattern = /^@([A-Za-z0-9_.-]+)(?: (\{.*\}))?$/s;
const placeholderPattern = /\{([A-Za-z0-9_]+)(?::([A-Za-z]+)(?::([^}]*))?)?\}/g;
const catalogs: Record<Language, Record<string, string>> = { uk: ukrainian, en: english };
const globalMessages = catalogs[activeLanguage];
const pluralCategories: Record<Language, Intl.LDMLPluralRule[]> = {
  uk: ['one', 'few', 'many'],
  en: ['one', 'other'],
};
const pluralRules = new Intl.PluralRules(activeLocale);

let strategyMessages: Record<string, string> = {};

export function useStrategyMessages(messages: Record<string, string>): void {
  strategyMessages = messages;
}

export function hasMessage(code: string): boolean {
  return templateOf(code) !== undefined;
}

export function hasUnknownMessage(value: unknown): boolean {
  if (typeof value === 'string') {
    const token = tokenPattern.exec(value);

    if (token === null) return false;
    if (templateOf(token[1]) === undefined) return true;

    const parameters = parametersOf(token[2]);

    return parameters !== null && hasUnknownMessage(parameters);
  }

  if (Array.isArray(value)) return value.some(hasUnknownMessage);

  if (value !== null && typeof value === 'object')
    return Object.values(value).some(hasUnknownMessage);

  return false;
}

export function translateCode(code: string, parameters: Parameters = {}): string {
  const template = templateOf(code);

  return template === undefined ? code : fill(template, parameters);
}

export function translate(text: string): string {
  const token = tokenPattern.exec(text);
  if (!token) return text;

  const template = templateOf(token[1]);
  if (template === undefined) return text;

  const parameters = parametersOf(token[2]);
  if (parameters === null) return text;

  return fill(template, parameters);
}

export function translateDeep<T>(value: T): T {
  if (typeof value === 'string') return translate(value) as T;

  if (Array.isArray(value)) return value.map((item) => translateDeep(item)) as T;

  if (value !== null && typeof value === 'object') {
    const entries = Object.entries(value).map(([key, item]) => [key, translateDeep(item)]);

    return Object.fromEntries(entries) as T;
  }

  return value;
}

function templateOf(code: string): string | undefined {
  return strategyMessages[code] ?? globalMessages[code];
}

function parametersOf(json: string | undefined): Parameters | null {
  if (!json) return {};

  try {
    return JSON.parse(json) as Parameters;
  } catch {
    return null;
  }
}

function fill(template: string, parameters: Parameters): string {
  return template.replace(
    placeholderPattern,
    (_, name: string, format?: string, argument?: string) => {
      const value = parameters[name];

      if (value === undefined || value === null) return '';

      return formatted(value, format, argument, parameters);
    },
  );
}

function formatted(
  value: unknown,
  format: string | undefined,
  argument: string | undefined,
  parameters: Parameters,
): string {
  switch (format) {
    case 'percent':
      return percent(Number(value));
    case 'signedPercent':
      return signedPercent(Number(value));
    case 'money':
      return money(Number(value), String(parameters[argument ?? ''] ?? ''));
    case 'amount':
      return Number(value).toFixed(2);
    case 'plural':
      return plural(Number(value), argument ?? '');
    case 'join':
      return (value as unknown[]).map(plainText).join(argument ?? ' ');
    default:
      return plainText(value);
  }
}

function plainText(value: unknown): string {
  if (Array.isArray(value)) return value.map(plainText).join(' ');

  return translate(String(value));
}

function percent(fraction: number): string {
  return `${Math.round(fraction * 100)}%`;
}

function signedPercent(fraction: number): string {
  const value = Math.round(fraction * 1000) / 10;

  return `${value < 0 ? '-' : '+'}${Math.abs(value).toFixed(1)}%`;
}

function money(amount: number, currency: string): string {
  const prefixes: Record<string, string> = { EUR: '€', USD: '$', GBP: '£', PLN: 'zł ' };
  const prefix = prefixes[currency] ?? `${currency} `;
  const digits = new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  return `${amount < 0 ? '-' : ''}${prefix}${digits.format(Math.abs(amount))}`;
}

function plural(count: number, forms: string): string {
  const options = forms.split('|');
  const category = pluralRules.select(Math.trunc(Math.abs(count)));
  const index = pluralCategories[activeLanguage].indexOf(category);

  return options[index < 0 ? options.length - 1 : index];
}

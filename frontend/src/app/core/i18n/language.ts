import { getBrowserLang } from '@jsverse/transloco';

export type Language = 'uk' | 'en';

export const languages: readonly Language[] = ['uk', 'en'];

const fallbackLanguage: Language = 'en';
const storageKey = 'investlab.language';

const locales: Record<Language, string> = { uk: 'uk-UA', en: 'en-GB' };

export const activeLanguage: Language = languageFor(rememberedLanguage());

export const activeLocale = locales[activeLanguage];

export function languageFor(choice: Language | null): Language {
  return choice ?? browserLanguage();
}

export function isLanguage(value: unknown): value is Language {
  return languages.includes(value as Language);
}

export function rememberedLanguage(): Language | null {
  try {
    const stored = localStorage.getItem(storageKey);

    return isLanguage(stored) ? stored : null;
  } catch {
    return null;
  }
}

export function rememberLanguage(language: Language | null): void {
  try {
    if (language === null) localStorage.removeItem(storageKey);
    else localStorage.setItem(storageKey, language);
  } catch {
    // The choice still lives in the profile; this browser just cannot keep a copy.
  }
}

function browserLanguage(): Language {
  const browser = getBrowserLang();

  return isLanguage(browser) ? browser : fallbackLanguage;
}

import { Injectable } from '@angular/core';
import { Translation, TranslocoLoader } from '@jsverse/transloco';
import { Language } from './language';

const catalogs: Record<Language, () => Promise<{ default: Translation }>> = {
  uk: () => import('../../../i18n/uk.json'),
  en: () => import('../../../i18n/en.json'),
};

@Injectable({ providedIn: 'root' })
export class CatalogLoader implements TranslocoLoader {
  async getTranslation(language: string): Promise<Translation> {
    const catalog = await catalogs[language as Language]();

    return catalog.default;
  }
}

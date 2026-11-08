import { Translation } from 'primeng/api';
import { Language } from '../i18n/language';
import { ukTranslation } from './uk-translation';

export const primeNgTranslations: Record<Language, Translation> = {
  uk: ukTranslation,
  en: {},
};

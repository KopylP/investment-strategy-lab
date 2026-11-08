import {
  inject,
  isDevMode,
  makeEnvironmentProviders,
  provideAppInitializer,
  provideEnvironmentInitializer,
} from '@angular/core';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { provideTranslocoMessageformat } from '@jsverse/transloco-messageformat';
import { TitleStrategy } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TranslatedTitleStrategy } from './translated-title-strategy';
import { ChosenLanguage } from './chosen-language';
import { activeLanguage, languages } from './language';
import { CatalogLoader } from './transloco-loader';

export function provideI18n() {
  return makeEnvironmentProviders([
    provideTransloco({
      config: {
        availableLangs: [...languages],
        defaultLang: activeLanguage,
        reRenderOnLangChange: false,
        prodMode: !isDevMode(),
      },
      loader: CatalogLoader,
    }),
    provideTranslocoMessageformat(),
    { provide: TitleStrategy, useClass: TranslatedTitleStrategy },
    provideEnvironmentInitializer(() => inject(ChosenLanguage)),
    provideAppInitializer(() => {
      document.documentElement.lang = activeLanguage;

      return firstValueFrom(inject(TranslocoService).load(activeLanguage));
    }),
  ]);
}

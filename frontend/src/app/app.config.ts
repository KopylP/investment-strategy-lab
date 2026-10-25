import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import { DialogService } from 'primeng/dynamicdialog';
import { routes } from './app.routes';
import { basePathInterceptor } from './core/api/base-path-interceptor';
import { unauthorizedInterceptor } from './core/auth/unauthorized-interceptor';
import { InvestmentLabPreset } from './core/theme/investment-lab.preset';
import { ukTranslation } from './core/theme/uk-translation';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideAnimationsAsync(),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    provideHttpClient(withFetch(), withInterceptors([basePathInterceptor, unauthorizedInterceptor])),
    providePrimeNG({
      theme: {
        preset: InvestmentLabPreset,
        options: { darkModeSelector: '.app-dark', cssLayer: { name: 'primeng', order: 'theme, base, primeng' } },
      },
      translation: ukTranslation,
    }),
    { provide: LOCALE_ID, useValue: 'uk' },
    MessageService,
    ConfirmationService,
    DialogService,
  ],
};

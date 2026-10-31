import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
  provideEnvironmentInitializer,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withNavigationErrorHandler,
} from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import { DialogService } from 'primeng/dynamicdialog';
import { routes } from './app.routes';
import { basePathInterceptor } from './core/api/base-path-interceptor';
import { reloadWithNewIngressSession } from './core/auth/ingress-session';
import { unauthorizedInterceptor } from './core/auth/unauthorized-interceptor';
import { NavigationHistory } from './core/layout/navigation-history';
import { InvestmentLabPreset } from './core/theme/investment-lab.preset';
import { ukTranslation } from './core/theme/uk-translation';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideAnimationsAsync(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled' }),
      withNavigationErrorHandler(reloadWithNewIngressSession),
    ),
    provideEnvironmentInitializer(() => inject(NavigationHistory)),
    provideHttpClient(
      withFetch(),
      withInterceptors([basePathInterceptor, unauthorizedInterceptor]),
    ),
    providePrimeNG({
      theme: {
        preset: InvestmentLabPreset,
        options: {
          darkModeSelector: '.app-dark',
          cssLayer: { name: 'primeng', order: 'theme, base, primeng' },
        },
      },
      translation: ukTranslation,
      overlayAppendTo: 'body',
    }),
    { provide: LOCALE_ID, useValue: 'uk' },
    MessageService,
    ConfirmationService,
    DialogService,
  ],
};

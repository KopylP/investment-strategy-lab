import { registerLocaleData } from '@angular/common';
import localeUk from '@angular/common/locales/uk';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

registerLocaleData(localeUk);

bootstrapApplication(App, appConfig).catch((error) => console.error(error));

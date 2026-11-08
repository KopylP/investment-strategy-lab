import { registerLocaleData } from '@angular/common';
import localeEnGb from '@angular/common/locales/en-GB';
import localeUk from '@angular/common/locales/uk';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

registerLocaleData(localeUk, 'uk-UA');
registerLocaleData(localeEnGb);

bootstrapApplication(App, appConfig).catch((error) => console.error(error));

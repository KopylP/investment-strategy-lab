import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { HomeAssistantPanelRoute } from './home-assistant-panel-route';

export function provideHomeAssistantPanel(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => inject(HomeAssistantPanelRoute).restore()),
  ]);
}

import { Routes } from '@angular/router';

export const signalsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/signals-page/signals-page').then((m) => m.SignalsPage),
    title: 'routes.signals',
  },
];

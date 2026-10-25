import { Routes } from '@angular/router';

export const holdingsRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/holdings-page/holdings-page').then((m) => m.HoldingsPage), title: 'Позиції' },
];

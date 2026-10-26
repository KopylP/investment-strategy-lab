import { Routes } from '@angular/router';

export const overviewRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/overview-page/overview-page').then((m) => m.OverviewPage), title: 'Огляд', data: { accountScope: 'all' } },
];

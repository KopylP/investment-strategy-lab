import { Routes } from '@angular/router';

export const taxesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/tax-year-page/tax-year-page').then((m) => m.TaxYearPage),
    title: 'Податки',
  },
];

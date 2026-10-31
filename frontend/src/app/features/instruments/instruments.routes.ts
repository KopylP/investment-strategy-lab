import { Routes } from '@angular/router';

export const instrumentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/instrument-list-page/instrument-list-page').then((m) => m.InstrumentListPage),
    title: 'Інструменти',
  },
];

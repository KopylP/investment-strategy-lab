import { Routes } from '@angular/router';

export const reportsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/report-list-page/report-list-page').then((m) => m.ReportListPage),
    title: 'routes.reports',
  },
  {
    path: ':reportId',
    loadComponent: () =>
      import('./pages/report-detail-page/report-detail-page').then((m) => m.ReportDetailPage),
    title: 'routes.report',
  },
];

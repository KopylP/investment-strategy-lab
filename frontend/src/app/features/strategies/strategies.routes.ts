import { Routes } from '@angular/router';

export const strategiesRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/strategy-list-page/strategy-list-page').then((m) => m.StrategyListPage), title: 'Стратегії' },
  { path: ':strategyId', loadComponent: () => import('./pages/strategy-detail-page/strategy-detail-page').then((m) => m.StrategyDetailPage), title: 'Стратегія' },
];

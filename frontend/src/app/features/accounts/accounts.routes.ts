import { Routes } from '@angular/router';

export const accountsRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/account-list-page/account-list-page').then((m) => m.AccountListPage), title: 'Рахунки' },
  { path: 'new', loadComponent: () => import('./pages/account-new-page/account-new-page').then((m) => m.AccountNewPage), title: 'Новий рахунок' },
  { path: ':accountId', loadComponent: () => import('./pages/account-detail-page/account-detail-page').then((m) => m.AccountDetailPage), title: 'Рахунок' },
];

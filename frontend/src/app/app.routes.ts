import { Routes } from '@angular/router';
import { signedInGuard } from './core/auth/auth-guards';
import { AppLayout } from './core/layout/app-layout';

export const routes: Routes = [
  { path: 'auth', loadChildren: () => import('./features/auth/auth.routes').then((m) => m.authRoutes) },
  {
    path: '',
    component: AppLayout,
    canActivate: [signedInGuard],
    children: [
      { path: '', pathMatch: 'full', loadChildren: () => import('./features/overview/overview.routes').then((m) => m.overviewRoutes) },
      { path: 'reports', loadChildren: () => import('./features/reports/reports.routes').then((m) => m.reportsRoutes) },
      { path: 'holdings', loadChildren: () => import('./features/holdings/holdings.routes').then((m) => m.holdingsRoutes) },
      { path: 'transactions', loadChildren: () => import('./features/transactions/transactions.routes').then((m) => m.transactionsRoutes) },
      { path: 'accounts', loadChildren: () => import('./features/accounts/accounts.routes').then((m) => m.accountsRoutes) },
      { path: 'strategies', loadChildren: () => import('./features/strategies/strategies.routes').then((m) => m.strategiesRoutes) },
      { path: 'instruments', loadChildren: () => import('./features/instruments/instruments.routes').then((m) => m.instrumentsRoutes) },
      { path: 'profile', loadComponent: () => import('./features/profile/pages/profile-page/profile-page').then((m) => m.ProfilePage), title: 'Профіль' },
    ],
  },
  { path: '**', redirectTo: '' },
];

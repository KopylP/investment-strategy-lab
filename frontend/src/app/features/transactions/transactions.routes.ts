import { Routes } from '@angular/router';

export const transactionsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/ledger-page/ledger-page').then((m) => m.LedgerPage),
    title: 'routes.transactions',
  },
  {
    path: 'import',
    loadComponent: () =>
      import('./pages/statement-import-page/statement-import-page').then(
        (m) => m.StatementImportPage,
      ),
    title: 'routes.revolutImport',
  },
];

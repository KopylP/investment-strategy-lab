import { Routes } from '@angular/router';

export const strategyBuilderRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/draft-list-page/draft-list-page').then((module) => module.DraftListPage),
    title: 'Конструктор',
  },
  {
    path: ':draftId/trials',
    loadComponent: () =>
      import('./pages/draft-trials-page/draft-trials-page').then(
        (module) => module.DraftTrialsPage,
      ),
    title: 'Бектести чернетки',
  },
  {
    path: ':draftId',
    loadComponent: () =>
      import('./pages/draft-editor-page/draft-editor-page').then(
        (module) => module.DraftEditorPage,
      ),
    title: 'Чернетка',
  },
];

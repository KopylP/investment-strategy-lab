import { Routes } from '@angular/router';
import { guestGuard } from '../../core/auth/auth-guards';

export const authRoutes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login-page/login-page').then((m) => m.LoginPage),
    title: 'routes.login',
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register-page/register-page').then((m) => m.RegisterPage),
    title: 'routes.register',
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
];

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

export const signedInGuard: CanActivateFn = async (_, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  return (await auth.ensureLoaded()) ? true : router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  return (await auth.ensureLoaded()) ? router.createUrlTree(['/']) : true;
};

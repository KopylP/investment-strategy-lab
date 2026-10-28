import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { DOCUMENT } from '@angular/common';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from './auth-store';

/** When the page last reloaded itself for a 401 in the Home Assistant panel (sessionStorage, per tab). */
const panelReloadKey = 'investmentlab.panelReloadAt';
/** A second 401 this soon after such a reload is not an expired Ingress session: fall back to the login page. */
const panelReloadCooldownMs = 60_000;

/**
 * A 401 on anything but the session check means the session is gone. Outside Home Assistant that is our cookie:
 * back to the login page. In the Home Assistant panel the user is signed in by the Supervisor's headers, so a 401
 * there is Home Assistant's own Ingress session (kept alive by its frontend, lapsed while the tab slept): a reload
 * makes Home Assistant open a new one.
 */
export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  const window = inject(DOCUMENT).defaultView;
  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !request.url.endsWith('/api/identity/me') && !request.url.endsWith('/login')) {
        if (auth.inHomeAssistantPanel && auth.isSignedIn() && window && reloadOnce(window)) return throwError(() => error);
        auth.signedOut();
        void router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};

/** Reloads the page unless it already did so for a 401 a moment ago (so a lasting 401 cannot loop). */
function reloadOnce(window: Window): boolean {
  try {
    const last = Number(window.sessionStorage.getItem(panelReloadKey));
    if (last && Date.now() - last < panelReloadCooldownMs) return false;
    window.sessionStorage.setItem(panelReloadKey, String(Date.now()));
  } catch {
    // No sessionStorage, so no way to tell a loop: the login page instead.
    return false;
  }
  window.location.reload();
  return true;
}

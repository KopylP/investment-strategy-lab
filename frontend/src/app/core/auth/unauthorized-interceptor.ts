import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthStore } from './auth-store';
import { IngressSession } from './ingress-session';

/** Already sent again after a new Ingress session: a second 401 is final. */
const retriedWithNewSession = new HttpContextToken(() => false);

/**
 * A 401 on anything but the session check means the session is gone. Outside Home Assistant that is our cookie:
 * back to the login page. In the Home Assistant panel the user is signed in by the Supervisor's headers, so a 401
 * there is Home Assistant's own Ingress session (lapsed while the phone slept): the request goes again once with a
 * new one. A password would not help there, so the panel never goes to the login page for it.
 */
export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  const ingress = inject(IngressSession);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || request.url.endsWith('/api/identity/me') || request.url.endsWith('/login'))
        return throwError(() => error);
      if (auth.inHomeAssistantPanel && auth.isSignedIn()) {
        if (request.context.get(retriedWithNewSession)) return throwError(() => error);
        return from(ingress.renew()).pipe(
          switchMap((renewed) => (renewed ? next(request.clone({ context: request.context.set(retriedWithNewSession, true) })) : throwError(() => error))),
        );
      }
      auth.signedOut();
      void router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url } });
      return throwError(() => error);
    }),
  );
};

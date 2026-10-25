import { HttpInterceptorFn } from '@angular/common/http';

/**
 * API calls are written as "/api/…"; when the app is served under a path (Home Assistant: "/investlab/" or an
 * Ingress path) they must go through it too. The page's <base href> knows the path.
 */
export const basePathInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) return next(request);
  const base = new URL(document.baseURI).pathname.replace(/\/$/, '');
  return next(base ? request.clone({ url: `${base}${request.url}` }) : request);
};

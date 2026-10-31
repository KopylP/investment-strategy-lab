import { HttpInterceptorFn } from '@angular/common/http';

export const basePathInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) return next(request);

  const base = new URL(document.baseURI).pathname.replace(/\/$/, '');

  return next(base ? request.clone({ url: `${base}${request.url}` }) : request);
};

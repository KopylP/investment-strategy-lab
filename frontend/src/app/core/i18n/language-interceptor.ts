import { HttpInterceptorFn } from '@angular/common/http';
import { activeLanguage } from './language';

export const languageInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) return next(request);

  return next(request.clone({ setHeaders: { 'Accept-Language': activeLanguage } }));
};

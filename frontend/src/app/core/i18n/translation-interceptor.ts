import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { map } from 'rxjs';
import { translateDeep } from './translate';

export const translationInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    map((event) => {
      if (!(event instanceof HttpResponse) || !isJsonBody(event.body)) return event;

      return event.clone({ body: translateDeep(event.body) });
    }),
  );

function isJsonBody(body: unknown): boolean {
  return Array.isArray(body) || (body !== null && typeof body === 'object' && !(body instanceof Blob) && !(body instanceof ArrayBuffer));
}

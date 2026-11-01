import { HttpClient, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, map, Observable, of, shareReplay, switchMap, tap } from 'rxjs';
import { hasUnknownMessage, translateDeep, useStrategyMessages } from './translate';

const strategyMessagesUrl = '/api/strategies/catalog/messages';

let loading: Observable<unknown> | null = null;

export const translationInterceptor: HttpInterceptorFn = (request, next) => {
  const http = inject(HttpClient);

  return next(request).pipe(
    switchMap((event) => {
      if (!(event instanceof HttpResponse) || !isJsonBody(event.body) || request.url.endsWith(strategyMessagesUrl)) return of(event);

      const ready = hasUnknownMessage(event.body) ? strategyMessages(http) : of(null);

      return ready.pipe(map(() => event.clone({ body: translateDeep(event.body) })));
    }),
  );
};

function strategyMessages(http: HttpClient): Observable<unknown> {
  loading ??= http.get<Record<string, string>>(strategyMessagesUrl).pipe(
    tap((messages) => useStrategyMessages(messages)),
    catchError(() => {
      loading = null;

      return of(null);
    }),
    shareReplay(1),
  );

  return loading;
}

function isJsonBody(body: unknown): boolean {
  return Array.isArray(body) || (body !== null && typeof body === 'object' && !(body instanceof Blob) && !(body instanceof ArrayBuffer));
}

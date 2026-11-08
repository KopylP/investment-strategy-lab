import { translate } from '@jsverse/transloco';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { hasMessage, translateCode } from '../i18n/translate';

type Query = Record<string, string | number | boolean | readonly string[] | null | undefined>;

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  get<T>(url: string, query?: Query): Promise<T> {
    return firstValueFrom(this.http.get<T>(url, { params: toParams(query) }));
  }

  post<T = void>(url: string, body: unknown = {}, query?: Query): Promise<T> {
    return firstValueFrom(this.http.post<T>(url, body, { params: toParams(query) }));
  }

  put<T = void>(url: string, body: unknown): Promise<T> {
    return firstValueFrom(this.http.put<T>(url, body));
  }

  delete<T = void>(url: string): Promise<T> {
    return firstValueFrom(this.http.delete<T>(url));
  }

  blob(url: string): Promise<Blob> {
    return firstValueFrom(this.http.get(url, { responseType: 'blob' }));
  }
}

function toParams(query?: Query): HttpParams | undefined {
  if (!query) return undefined;

  let params = new HttpParams();

  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined) continue;

    if (Array.isArray(value)) value.forEach((item) => (params = params.append(key, item)));
    else params = params.set(key, String(value));
  }

  return params;
}

export function toProblemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as {
      code?: string;
      parameters?: Record<string, unknown> | null;
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    } | null;

    if (problem?.code && hasMessage(problem.code)) return translateCode(problem.code, problem.parameters ?? {});

    if (problem?.errors) return Object.values(problem.errors).flat().join(' ');

    if (problem?.detail) return problem.detail;

    if (error.status === 0) return translate('common.serverUnavailable');

    return problem?.title ?? translate<string>('common.errorStatus', { status: error.status });
  }

  return error instanceof Error ? error.message : translate('common.unknownError');
}

export function problemCode(error: unknown): string | undefined {
  return error instanceof HttpErrorResponse
    ? (error.error as { code?: string } | null)?.code
    : undefined;
}

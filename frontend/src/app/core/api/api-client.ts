import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

type Query = Record<string, string | number | boolean | readonly string[] | null | undefined>;

/** Thin promise-based wrapper: stores await commands, resources await queries. */
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

  /** A file the API sends (a PDF), as a blob to hand to the browser. */
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

/** RFC 9457 problem details from the API, in words for a toast. */
/** The API speaks English; the codes users meet most often get Ukrainian words. */
const messages: Record<string, string> = {
  'user.invalid_credentials': 'Невірний email або пароль.',
  'user.locked_out': 'Забагато невдалих спроб. Спробуйте за кілька хвилин.',
  'user.DuplicateUserName': 'Такий email уже зареєстровано.',
  'user.DuplicateEmail': 'Такий email уже зареєстровано.',
  'user.PasswordTooShort': 'Пароль має містити щонайменше 10 символів.',
  'report.no_strategy_answered': 'Жодна стратегія не змогла оцінити рахунок. Найчастіше бракує котирувань — оновіть їх на сторінці «Інструменти й котирування».',
  'report.no_sub_portfolios': 'На рахунку немає під-портфеля зі стратегією. Призначте стратегію на сторінці рахунку.',
  'report.not_open': 'Рапорт уже закрито або замінено — відмічати в ньому не можна.',
  'report.action_resolved': 'Дію вже відмічено. Спершу скасуйте відмітку.',
  'report.action_not_pending': 'Пропустити можна лише дію, яку ще не почали виконувати.',
  'account.target_shares_not_100': 'Цільові частки мають скластися рівно в 100%.',
  'account.sub_portfolio_name_taken': 'Під-портфель з такою назвою вже є.',
  'strategy.name_taken': 'Стратегія з такою назвою вже є.',
  'transaction.not_enough_units': 'У під-портфелі немає стільки штук на цю дату.',
  'transaction.nothing_to_sell': 'У під-портфелі немає цього ETF.',
  'transaction.already_reversed': 'Операцію вже сторновано.',
  'notification.no_channel': 'Сповіщення працюють лише в Home Assistant.',
};

export function toProblemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const code = (error.error as { code?: string } | null)?.code;
    if (code && messages[code]) return messages[code];
    const problem = error.error as { detail?: string; title?: string; errors?: Record<string, string[]> } | null;
    if (problem?.errors) return Object.values(problem.errors).flat().join(' ');
    if (problem?.detail) return problem.detail;
    if (error.status === 0) return 'Сервер недоступний. Перевірте, чи запущено API.';
    return problem?.title ?? `Помилка ${error.status}`;
  }
  return error instanceof Error ? error.message : 'Невідома помилка';
}

export function problemCode(error: unknown): string | undefined {
  return error instanceof HttpErrorResponse ? (error.error as { code?: string } | null)?.code : undefined;
}

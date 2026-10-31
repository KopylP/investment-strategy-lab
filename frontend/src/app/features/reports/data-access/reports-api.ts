import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { FillInput } from '../models/report-vm';
import {
  LoadOperationsPreviewDto,
  PlanSettingsDto,
  ReportDetailsDto,
  ReportRequestDto,
  ReportSummaryDto,
} from '../models/report-dto';
import { ReportSchedule } from '../models/report-schedule';

@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private readonly api = inject(ApiClient);

  list(accountId: string): Promise<ReportSummaryDto[]> {
    return this.api.get<ReportSummaryDto[]>('/api/rebalancing/reports', { accountId });
  }

  get(reportId: string): Promise<ReportDetailsDto> {
    return this.api.get<ReportDetailsDto>(`/api/rebalancing/reports/${reportId}`);
  }

  schedule(accountId: string): Promise<ReportSchedule> {
    return this.api.get<ReportSchedule>(`/api/rebalancing/accounts/${accountId}/report-schedule`);
  }

  generate(
    accountId: string,
    code: string,
    contribution: number | null = null,
  ): Promise<{ requestId: string }> {
    return this.api.post(`/api/rebalancing/accounts/${accountId}/reports`, { code, contribution });
  }

  requests(accountId: string): Promise<ReportRequestDto[]> {
    return this.api.get<ReportRequestDto[]>('/api/rebalancing/report-requests', { accountId });
  }

  settings(accountId: string): Promise<PlanSettingsDto> {
    return this.api.get<PlanSettingsDto>(`/api/rebalancing/accounts/${accountId}/settings`);
  }

  markExecuted(reportId: string, actionId: string, fill: FillInput): Promise<void> {
    return this.api.post(`/api/rebalancing/reports/${reportId}/actions/${actionId}/fills`, fill);
  }

  recordPartialFill(reportId: string, actionId: string, fill: FillInput): Promise<void> {
    return this.api.post(
      `/api/rebalancing/reports/${reportId}/actions/${actionId}/partial-fills`,
      fill,
    );
  }

  skip(reportId: string, actionId: string, reason: string): Promise<void> {
    return this.api.post(`/api/rebalancing/reports/${reportId}/actions/${actionId}/skip`, {
      reason,
    });
  }

  undo(reportId: string, actionId: string): Promise<void> {
    return this.api.post(`/api/rebalancing/reports/${reportId}/actions/${actionId}/undo`);
  }

  previewJournalMatches(reportId: string): Promise<LoadOperationsPreviewDto> {
    return this.api.get<LoadOperationsPreviewDto>(
      `/api/rebalancing/reports/${reportId}/journal-matches`,
    );
  }

  applyJournalMatches(reportId: string): Promise<number> {
    return this.api.post<number>(`/api/rebalancing/reports/${reportId}/journal-matches`);
  }

  close(reportId: string): Promise<void> {
    return this.api.post(`/api/rebalancing/reports/${reportId}/close`);
  }

  ingestQuotes(): Promise<unknown> {
    return this.api.post('/api/market-data/quotes/ingest');
  }
}

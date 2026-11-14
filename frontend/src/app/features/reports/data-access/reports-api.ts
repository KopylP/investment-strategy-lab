import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { EnteredFill } from '../models/report-vm';
import {
  GenerateReportBody,
  IngestReport,
  LoadOperationsPreviewDto,
  MarkActionExecutedBody,
  PlanSettingsDto,
  RecordPartialFillBody,
  ReportDetailsDto,
  ReportRequestDto,
  ReportRequestedDto,
  ReportSummaryDto,
  SkipActionBody,
} from '../models/report-dto';
import { ReportSchedule } from '../models/report-schedule-dto';

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
  ): Promise<ReportRequestedDto> {
    const body: GenerateReportBody = { code, contribution };

    return this.api.post<ReportRequestedDto>(
      `/api/rebalancing/accounts/${accountId}/reports`,
      body,
    );
  }

  requests(accountId: string): Promise<ReportRequestDto[]> {
    return this.api.get<ReportRequestDto[]>('/api/rebalancing/report-requests', { accountId });
  }

  settings(accountId: string): Promise<PlanSettingsDto> {
    return this.api.get<PlanSettingsDto>(`/api/rebalancing/accounts/${accountId}/settings`);
  }

  markExecuted(reportId: string, actionId: string, fill: EnteredFill): Promise<void> {
    const body: MarkActionExecutedBody = fill;

    return this.api.post(`/api/rebalancing/reports/${reportId}/actions/${actionId}/fills`, body);
  }

  recordPartialFill(reportId: string, actionId: string, fill: EnteredFill): Promise<void> {
    const body: RecordPartialFillBody = fill;

    return this.api.post(
      `/api/rebalancing/reports/${reportId}/actions/${actionId}/partial-fills`,
      body,
    );
  }

  skip(reportId: string, actionId: string, reason: string): Promise<void> {
    const body: SkipActionBody = { reason };

    return this.api.post(`/api/rebalancing/reports/${reportId}/actions/${actionId}/skip`, body);
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

  ingestQuotes(): Promise<IngestReport> {
    return this.api.post<IngestReport>('/api/market-data/quotes/ingest');
  }
}

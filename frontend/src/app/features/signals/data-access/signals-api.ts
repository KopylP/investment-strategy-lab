import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { ChangeSignalBody, CreateSignalBody, SignalReportRuleDto, SignalsOverviewDto } from '../models/signal-dto';

@Injectable({ providedIn: 'root' })
export class SignalsApi {
  private readonly api = inject(ApiClient);

  overview(): Promise<SignalsOverviewDto> {
    return this.api.get<SignalsOverviewDto>('/api/signals');
  }

  create(body: CreateSignalBody): Promise<{ id: string }> {
    return this.api.post('/api/signals', body);
  }

  change(signalId: string, body: ChangeSignalBody): Promise<void> {
    return this.api.put(`/api/signals/${signalId}`, body);
  }

  delete(signalId: string): Promise<void> {
    return this.api.delete(`/api/signals/${signalId}`);
  }

  check(signalId: string): Promise<void> {
    return this.api.post(`/api/signals/${signalId}/check`);
  }

  reportRules(): Promise<SignalReportRuleDto[]> {
    return this.api.get<SignalReportRuleDto[]>('/api/rebalancing/signal-report-rules');
  }

  addReportRule(signalId: string, accountId: string, reportCode: string): Promise<SignalReportRuleDto> {
    return this.api.post('/api/rebalancing/signal-report-rules', { signalId, accountId, reportCode });
  }

  removeReportRule(ruleId: string): Promise<void> {
    return this.api.delete(`/api/rebalancing/signal-report-rules/${ruleId}`);
  }

  mutedSignals(): Promise<string[]> {
    return this.api.get<string[]>('/api/notifications/muted-signals');
  }

  setNotify(signalId: string, notify: boolean): Promise<void> {
    return this.api.put(`/api/notifications/signals/${signalId}`, { notify });
  }
}

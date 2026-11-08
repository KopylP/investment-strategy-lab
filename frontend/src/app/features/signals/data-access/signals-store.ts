import { translate } from '@jsverse/transloco';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { ReportsApi } from '../../reports/data-access/reports-api';
import { distinctReports, ScheduledReport } from '../../reports/models/report-schedule';
import { SignalDto, SignalReportRuleDto } from '../models/signal-dto';
import { conditionOf, createBodyOf, SignalForm } from '../models/signal-form';
import { SignalsApi } from './signals-api';

@Injectable()
export class SignalsStore {
  private readonly api = inject(SignalsApi);
  private readonly reportsApi = inject(ReportsApi);
  private readonly messages = inject(MessageService);
  private readonly accountContext = inject(AccountContext);
  private readonly catalog = inject(InstrumentCatalog);

  private readonly overviewResource = resource({ loader: () => this.api.overview() });
  private readonly mutedResource = resource({ loader: () => this.api.mutedSignals() });
  private readonly rulesResource = resource({ loader: () => this.api.reportRules() });
  private readonly reportsResource = resource({
    params: () => this.accountContext.accounts().map((account) => account.id),
    loader: ({ params }) => this.loadReports(params),
  });

  readonly isLoading = this.overviewResource.isLoading;
  readonly loadError = computed(() =>
    this.overviewResource.error() ? toProblemMessage(this.overviewResource.error()) : null,
  );
  readonly saving = signal(false);

  readonly signals = computed(() => this.overviewResource.value()?.signals ?? []);
  readonly active = computed(() => this.overviewResource.value()?.active ?? []);
  readonly recent = computed(() => this.overviewResource.value()?.recent ?? []);
  readonly accounts = this.accountContext.accounts;
  readonly instruments = this.catalog.instruments;

  private readonly muted = computed(() => new Set(this.mutedResource.value() ?? []));
  private readonly rules = computed(() => this.rulesResource.value() ?? []);
  private readonly reportsByAccount = computed(
    () => this.reportsResource.value() ?? new Map<string, ScheduledReport[]>(),
  );

  notifies(signalId: string): boolean {
    return !this.muted().has(signalId);
  }

  rulesOf(signalId: string): SignalReportRuleDto[] {
    return this.rules().filter((rule) => rule.signalId === signalId);
  }

  reportsOf(accountId: string | null): ScheduledReport[] {
    return accountId ? (this.reportsByAccount().get(accountId) ?? []) : [];
  }

  ruleLabel(rule: SignalReportRuleDto): string {
    const report = this.reportsOf(rule.accountId).find((each) => each.code === rule.reportCode);
    const account = this.accounts().find((each) => each.id === rule.accountId);

    return `${report?.name ?? rule.reportCode} · ${account?.name ?? translate('signals.accountFallback')}`;
  }

  targetLabel(signal: SignalDto): string {
    const account = this.accounts().find((each) => each.id === signal.accountId);

    switch (signal.target) {
      case 'Instrument':
        return this.catalog.symbol(signal.instrumentId);
      case 'InstrumentGroup':
        return signal.groupName ?? translate('signals.subject.group');
      case 'SubPortfolio':
        return (
          account?.subPortfolios.find((sub) => sub.id === signal.subPortfolioId)?.name ??
          translate('signals.editor.subPortfolio')
        );
      case 'EachPosition':
        return translate('signals.eachPosition', {
          account: account?.name ?? translate('signals.accountFallback'),
        });
      default:
        return account?.name ?? translate('signals.editor.account');
    }
  }

  create(form: SignalForm): Promise<boolean> {
    return this.run(translate('signals.created'), translate('signals.notCreated'), () =>
      this.api.create(createBodyOf(form)),
    );
  }

  change(signalId: string, form: SignalForm): Promise<boolean> {
    const body = {
      name: form.name,
      condition: conditionOf(form),
      schedule: form.schedule,
      enabled: form.enabled,
    };

    return this.run(translate('signals.saved'), translate('signals.notSaved'), () =>
      this.api.change(signalId, body),
    );
  }

  delete(signalId: string): Promise<boolean> {
    return this.run(translate('signals.deleted'), translate('signals.notDeleted'), () => this.api.delete(signalId));
  }

  check(signalId: string): Promise<boolean> {
    return this.run(translate('signals.checked'), translate('signals.checkFailed'), () =>
      this.api.check(signalId),
    );
  }

  addReportRule(signalId: string, accountId: string, reportCode: string): Promise<boolean> {
    return this.run(translate('signals.reportConnected'), translate('signals.reportNotConnected'), () =>
      this.api.addReportRule(signalId, accountId, reportCode),
    );
  }

  removeReportRule(ruleId: string): Promise<boolean> {
    return this.run(translate('signals.reportDisconnected'), translate('signals.reportNotDisconnected'), () =>
      this.api.removeReportRule(ruleId),
    );
  }

  setNotify(signalId: string, notify: boolean): Promise<boolean> {
    return this.run(notify ? translate('signals.notificationsOn') : translate('signals.notificationsOff'), translate('profile.notSaved'), () =>
      this.api.setNotify(signalId, notify),
    );
  }

  private async run(
    success: string,
    failure: string,
    action: () => Promise<unknown>,
  ): Promise<boolean> {
    this.saving.set(true);

    try {
      await action();
      this.messages.add({ severity: 'success', summary: success });
      this.reload();

      return true;
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: failure,
        detail: toProblemMessage(error),
        life: 8000,
      });

      return false;
    } finally {
      this.saving.set(false);
    }
  }

  private reload(): void {
    this.overviewResource.reload();
    this.mutedResource.reload();
    this.rulesResource.reload();
  }

  private async loadReports(accountIds: string[]): Promise<Map<string, ScheduledReport[]>> {
    const entries = await Promise.all(accountIds.map((accountId) => this.loadReportsOf(accountId)));

    return new Map(entries);
  }

  private async loadReportsOf(accountId: string): Promise<[string, ScheduledReport[]]> {
    const schedule = await this.reportsApi.schedule(accountId);

    return [accountId, distinctReports(schedule.reports)];
  }
}

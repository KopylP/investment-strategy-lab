import { translate } from '@jsverse/transloco';
import { computed, effect, inject, Injectable, resource, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { formatMoney } from '../../../shared/pipes/format';
import { StrategiesApi } from '../../strategies/data-access/strategies-api';
import { editableTiming, isTimingComplete, reportIcon, scheduleLabel } from '../../reports/models/report-schedule';
import { ReportTiming } from '../../reports/models/report-schedule-dto';
import { ReportDefinition, Strategy } from '../../strategies/models/strategy';
import { describeStrategy } from '../../strategies/models/strategy-summary';
import { PlanSettingsVm } from '../ui/plan-settings-form/plan-settings-form';
import { NewSubPortfolioDto } from '../models/account-dto';
import { AccountsApi } from './accounts-api';

export interface SleeveDraft {
  key: number;
  name: string;
  targetSharePercent: number;
  strategyId: string | null;
}

type Preset = 'simple' | 'balanced' | 'custom';

@Injectable()
export class NewAccountDraft {
  private readonly api = inject(AccountsApi);
  private readonly strategiesApi = inject(StrategiesApi);
  private readonly accounts = inject(AccountContext);
  private readonly messages = inject(MessageService);
  private readonly router = inject(Router);
  private readonly instruments = inject(InstrumentCatalog);
  private nextKey = 1;

  private readonly strategiesResource = resource({
    loader: async () => {
      let strategies = await this.strategiesApi.list();
      if (!strategies.some((strategy) => strategy.status === 'Active')) {
        await this.strategiesApi.createDefaults();
        strategies = await this.strategiesApi.list();
      }

      return strategies;
    },
  });

  private readonly catalogResource = resource({ loader: () => this.strategiesApi.catalog() });

  readonly step = signal(1);
  readonly name = signal(translate('accounts.wizard.defaultAccountName'));
  readonly broker = signal('Revolut');
  readonly currency = signal<'EUR' | 'PLN' | 'USD'>('EUR');
  private readonly chosenTaxPolicy = signal<string | null>(null);
  readonly taxPolicy = computed(
    () => this.chosenTaxPolicy() ?? this.taxPolicies().find((policy) => policy.isDefault)?.value ?? '',
  );
  readonly preset = signal<Preset>('simple');
  readonly sleeves = signal<SleeveDraft[]>([]);
  readonly plan = signal<PlanSettingsVm>({
    monthlyContribution: 200,
    minTrade: 25,
    trimBandPercent: 10,
    allowFractionalShares: true,
    levelByPurchasesOnly: false,
    generateOnSchedule: true,
    contributionPolicy: 'FillUnderweight',
    driftBandPercent: 5,
  });
  readonly ownTimes = signal<Record<string, ReportTiming>>({});
  readonly showAdvanced = signal(false);
  readonly saving = signal(false);

  readonly brokers = ['Revolut', 'XTB', 'Interactive Brokers', 'Trading 212', translate('accounts.wizard.otherBroker')].map(
    (value) => ({ label: value, value }),
  );
  readonly currencies = ['EUR', 'PLN', 'USD'].map((value) => ({ label: value, value }));
  private readonly taxPoliciesResource = resource({ loader: () => this.api.taxPolicies() });

  readonly taxPolicies = computed(() =>
    (this.taxPoliciesResource.value() ?? []).map((policy) => ({
      label: policy.name,
      value: policy.code,
      hint: policy.hint,
      isDefault: policy.isDefault,
    })),
  );

  readonly strategies = computed(() =>
    (this.strategiesResource.value() ?? []).filter((strategy) => strategy.status === 'Active'),
  );
  chooseTaxPolicy(code: string): void {
    this.chosenTaxPolicy.set(code);
  }

  readonly strategiesLoading = this.strategiesResource.isLoading;
  readonly taxHint = computed(
    () => this.taxPolicies().find((policy) => policy.value === this.taxPolicy())?.hint ?? '',
  );
  readonly total = computed(
    () =>
      Math.round(
        this.sleeves().reduce((sum, sleeve) => sum + (sleeve.targetSharePercent ?? 0), 0) * 100,
      ) / 100,
  );

  readonly accountValid = computed(() => !!this.name().trim() && !!this.broker().trim());
  readonly sleevesValid = computed(
    () =>
      this.sleeves().length > 0 &&
      this.total() === 100 &&
      this.sleeves().every(
        (sleeve) => sleeve.name.trim() && sleeve.targetSharePercent > 0 && sleeve.strategyId,
      ),
  );

  readonly reports = computed(() => {
    const strategies = new Map(this.strategies().map((strategy) => [strategy.id, strategy]));
    const types = new Map((this.catalogResource.value() ?? []).map((type) => [type.code, type]));
    const reports = new Map<string, ReportDefinition & { strategyTiming: ReportTiming }>();

    for (const sleeve of this.sleeves()) {
      const strategy = sleeve.strategyId ? strategies.get(sleeve.strategyId) : undefined;

      for (const report of (strategy && types.get(strategy.typeCode)?.reports) || []) {
        if (!reports.has(report.code))
          reports.set(report.code, {
            ...report,
            strategyTiming: editableTiming(
              report.schedule ?? {
                frequency: 'off',
                day: 1,
                weekday: 'sunday',
                time: '20:00',
                minute: 0,
              },
            ),
          });
      }
    }

    return [...reports.values()].map((report) => {
      const own = this.ownTimes()[report.code] ?? null;

      return { ...report, own, timing: own ?? report.strategyTiming };
    });
  });
  readonly scheduleValid = computed(() =>
    this.reports().every((report) => !report.own || isTimingComplete(report.own)),
  );
  readonly scheduleLabel = scheduleLabel;
  readonly reportIcon = reportIcon;
  readonly timingChoices = [
    { label: translate('accounts.asInStrategyLabel'), value: false },
    { label: translate('accounts.wizard.ownTime'), value: true },
  ];

  readonly summary = computed(() => {
    const byId = new Map(this.strategies().map((strategy) => [strategy.id, strategy]));

    return this.sleeves().map((sleeve) => {
      const strategy = sleeve.strategyId ? byId.get(sleeve.strategyId) : undefined;

      return {
        name: sleeve.name,
        share: sleeve.targetSharePercent,
        contribution: formatMoney(
          (this.plan().monthlyContribution * sleeve.targetSharePercent) / 100,
          this.currency(),
          0,
        ),
        strategy: strategy?.name ?? '—',
        blurb: strategy ? this.blurb(strategy).headline : '',
      };
    });
  });

  constructor() {
    effect(() => {
      if (this.strategies().length)
        untracked(() => !this.sleeves().length && this.choosePreset(this.preset()));
    });
  }

  choosePreset(preset: Preset): void {
    this.preset.set(preset);
    const strategies = this.strategies();
    const eu = strategies.find((strategy) => strategy.name.startsWith('EU')) ?? strategies[0];
    const us =
      strategies.find((strategy) => strategy.name.startsWith('US')) ??
      strategies[1] ??
      strategies[0];
    if (preset === 'simple') this.sleeves.set([this.sleeve(translate('accounts.wizard.mainSleeve'), 100, eu?.id ?? null)]);
    else if (preset === 'balanced')
      this.sleeves.set([
        this.sleeve(translate('accounts.wizard.europe'), 70, eu?.id ?? null),
        this.sleeve(translate('accounts.wizard.usa'), 30, us?.id ?? null),
      ]);
    else if (!this.sleeves().length)
      this.sleeves.set([this.sleeve(translate('accounts.wizard.mainSleeve'), 100, eu?.id ?? null)]);
  }

  update(key: number, change: Partial<SleeveDraft>): void {
    this.preset.set('custom');
    this.sleeves.update((sleeves) =>
      sleeves.map((sleeve) => (sleeve.key === key ? { ...sleeve, ...change } : sleeve)),
    );
  }

  add(): void {
    this.preset.set('custom');
    this.sleeves.update((sleeves) => [
      ...sleeves,
      this.sleeve(
        translate('accounts.wizard.defaultSleeveName', { number: sleeves.length + 1 }),
        Math.max(0, 100 - this.total()),
        this.strategies()[0]?.id ?? null,
      ),
    ]);
  }

  remove(key: number): void {
    this.sleeves.update((sleeves) => sleeves.filter((sleeve) => sleeve.key !== key));
  }

  balance(): void {
    const sleeves = this.sleeves();
    if (!sleeves.length) return;

    const others = sleeves.slice(0, -1).reduce((sum, sleeve) => sum + sleeve.targetSharePercent, 0);

    this.update(sleeves.at(-1)!.key, {
      targetSharePercent: Math.max(1, Math.round((100 - others) * 100) / 100),
    });
  }

  useOwn(code: string, strategyTiming: ReportTiming, own: boolean): void {
    this.ownTimes.update((times) => {
      const { [code]: _, ...rest } = times;

      return own ? { ...rest, [code]: times[code] ?? strategyTiming } : rest;
    });
  }

  setOwn(code: string, timing: ReportTiming): void {
    this.ownTimes.update((times) => ({ ...times, [code]: timing }));
  }

  blurb(strategy: Strategy) {
    return describeStrategy(
      strategy,
      (id) => this.instruments.symbol(id),
      (id) => this.instruments.byId().get(id)?.tradingCurrency,
    );
  }

  async save(): Promise<void> {
    if (!this.accountValid() || !this.sleevesValid()) return;

    this.saving.set(true);

    try {
      const account = await this.openAccount();

      await this.saveInitialPlan(account.id);
      await this.saveOwnTimes(account.id);
      this.accounts.reload();
      this.accounts.choose(account.id);
      this.createdId.set(account.id);
      this.step.set(6);
    } catch (error) {
      this.reportNotOpened(error);
    } finally {
      this.saving.set(false);
    }
  }

  private reportNotOpened(error: unknown): void {
    this.messages.add({
      severity: 'error',
      summary: translate('accounts.wizard.notCreated'),
      detail: toProblemMessage(error),
      life: 8000,
    });
  }

  private openAccount() {
    const plan = this.plan();

    return this.api.open({
      name: this.name().trim(),
      broker: this.broker().trim(),
      baseCurrency: this.currency(),
      taxPolicyCode: this.taxPolicy(),
      subPortfolios: this.sleeves().map((sleeve): NewSubPortfolioDto => ({
        name: sleeve.name.trim(),
        targetSharePercent: sleeve.targetSharePercent,
        strategyId: sleeve.strategyId,
        cashInstrumentId: null,
        maxPositionWeightPercent: null,
      })),
      contributionPolicy: {
        kind: plan.contributionPolicy,
        driftBandPercent: plan.driftBandPercent,
      },
    });
  }

  private saveInitialPlan(accountId: string) {
    const plan = this.plan();

    return this.api.changePlanSettings(accountId, {
      monthlyContribution: plan.monthlyContribution,
      minTrade: plan.minTrade,
      trimBand: plan.trimBandPercent / 100,
      allowFractionalShares: plan.allowFractionalShares,
      levelByPurchasesOnly: plan.levelByPurchasesOnly,
      generateOnSchedule: plan.generateOnSchedule,
    });
  }

  readonly createdId = signal<string | null>(null);

  private async saveOwnTimes(accountId: string): Promise<void> {
    for (const report of this.reports()) {
      if (!report.own) continue;

      try {
        await this.api.changeReportSchedule(accountId, report.code, report.own);
      } catch (error) {
        this.messages.add({
          severity: 'warn',
          summary: translate('accounts.wizard.timeNotSaved', { report: report.name }),
          life: 10000,
          detail: translate('accounts.wizard.timeNotSavedHint', { error: toProblemMessage(error) }),
        });
      }
    }
  }

  async go(path: string[]): Promise<void> {
    await this.router.navigate(path, { replaceUrl: true });
  }

  private sleeve(name: string, share: number, strategyId: string | null): SleeveDraft {
    return { key: this.nextKey++, name, targetSharePercent: share, strategyId };
  }
}

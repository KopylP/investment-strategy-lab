import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  resource,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { SelectButton } from 'primeng/selectbutton';
import { Slider } from 'primeng/slider';
import { Step, StepList, StepPanel, StepPanels, Stepper } from 'primeng/stepper';
import { toProblemMessage } from '../../../../core/api/api-client';
import { AccountContext } from '../../../../core/context/account-context';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { formatMoney } from '../../../../shared/pipes/format';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { StrategiesApi } from '../../../strategies/data-access/strategies-api';
import {
  editableTiming,
  isTimingComplete,
  reportIcon,
  ReportTiming,
  scheduleLabel,
} from '../../../reports/models/report-schedule';
import { ReportTimingEditor } from '../../../reports/ui/report-timing-editor/report-timing-editor';
import { ReportDefinition, Strategy } from '../../../strategies/models/strategy';
import { describeStrategy } from '../../../strategies/models/strategy-summary';
import { StrategyPicker } from '../../../strategies/ui/strategy-picker/strategy-picker';
import { AccountsApi, SubPortfolioInput } from '../../data-access/accounts-api';
import { PlanSettingsForm, PlanSettingsVm } from '../../ui/plan-settings-form/plan-settings-form';

interface SleeveDraft {
  key: number;
  name: string;
  targetSharePercent: number;
  strategyId: string | null;
}

type Preset = 'simple' | 'balanced' | 'custom';

@Component({
  selector: 'app-account-new-page',
  imports: [
    FormsModule,
    Button,
    InputNumber,
    InputText,
    Message,
    SelectButton,
    Slider,
    Stepper,
    StepList,
    Step,
    StepPanels,
    StepPanel,
    PageHeader,
    PlanSettingsForm,
    ReportTimingEditor,
    StrategyPicker,
  ],
  templateUrl: './account-new-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountNewPage {
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

  protected readonly step = signal(1);
  protected readonly name = signal('Брокерський рахунок');
  protected readonly broker = signal('Revolut');
  protected readonly currency = signal<'EUR' | 'PLN' | 'USD'>('EUR');
  protected readonly taxPolicy = signal('PL-BELKA');
  protected readonly preset = signal<Preset>('simple');
  protected readonly sleeves = signal<SleeveDraft[]>([]);
  protected readonly plan = signal<PlanSettingsVm>({
    monthlyContribution: 200,
    minTrade: 25,
    trimBandPercent: 10,
    allowFractionalShares: true,
    generateOnSchedule: true,
    contributionPolicy: 'FillUnderweight',
    driftBandPercent: 5,
  });
  protected readonly ownTimes = signal<Record<string, ReportTiming>>({});
  protected readonly showAdvanced = signal(false);
  protected readonly saving = signal(false);

  protected readonly brokers = [
    'Revolut',
    'XTB',
    'Interactive Brokers',
    'Trading 212',
    'Інший',
  ].map((value) => ({ label: value, value }));
  protected readonly currencies = ['EUR', 'PLN', 'USD'].map((value) => ({ label: value, value }));
  protected readonly taxPolicies = [
    {
      label: 'Звичайний (Белка 19%)',
      value: 'PL-BELKA',
      hint: 'Податок 19% з прибутку від продажів і дивідендів, рахується за FIFO в злотих.',
    },
    {
      label: 'IKE',
      value: 'PL-IKE',
      hint: 'Пенсійний рахунок: без податку з прибутку, якщо не виводити до пенсії.',
    },
    {
      label: 'IKZE',
      value: 'PL-IKZE',
      hint: 'Пенсійний рахунок з податковою знижкою на внески; податок лише при виході.',
    },
  ];

  protected readonly strategies = computed(() =>
    (this.strategiesResource.value() ?? []).filter((strategy) => strategy.status === 'Active'),
  );
  protected readonly strategiesLoading = this.strategiesResource.isLoading;
  protected readonly taxHint = computed(
    () => this.taxPolicies.find((policy) => policy.value === this.taxPolicy())?.hint ?? '',
  );
  protected readonly total = computed(
    () =>
      Math.round(
        this.sleeves().reduce((sum, sleeve) => sum + (sleeve.targetSharePercent ?? 0), 0) * 100,
      ) / 100,
  );

  protected readonly accountValid = computed(() => !!this.name().trim() && !!this.broker().trim());
  protected readonly sleevesValid = computed(
    () =>
      this.sleeves().length > 0 &&
      this.total() === 100 &&
      this.sleeves().every(
        (sleeve) => sleeve.name.trim() && sleeve.targetSharePercent > 0 && sleeve.strategyId,
      ),
  );

  protected readonly reports = computed(() => {
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
  protected readonly scheduleValid = computed(() =>
    this.reports().every((report) => !report.own || isTimingComplete(report.own)),
  );
  protected readonly scheduleLabel = scheduleLabel;
  protected readonly reportIcon = reportIcon;
  protected readonly timingChoices = [
    { label: 'Як у стратегії', value: false },
    { label: 'Свій час', value: true },
  ];

  protected readonly summary = computed(() => {
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

  protected choosePreset(preset: Preset): void {
    this.preset.set(preset);
    const strategies = this.strategies();
    const eu = strategies.find((strategy) => strategy.name.startsWith('EU')) ?? strategies[0];
    const us =
      strategies.find((strategy) => strategy.name.startsWith('US')) ??
      strategies[1] ??
      strategies[0];
    if (preset === 'simple') this.sleeves.set([this.sleeve('Основний', 100, eu?.id ?? null)]);
    else if (preset === 'balanced')
      this.sleeves.set([
        this.sleeve('Європа', 70, eu?.id ?? null),
        this.sleeve('США', 30, us?.id ?? null),
      ]);
    else if (!this.sleeves().length)
      this.sleeves.set([this.sleeve('Основний', 100, eu?.id ?? null)]);
  }

  protected update(key: number, change: Partial<SleeveDraft>): void {
    this.preset.set('custom');
    this.sleeves.update((sleeves) =>
      sleeves.map((sleeve) => (sleeve.key === key ? { ...sleeve, ...change } : sleeve)),
    );
  }

  protected add(): void {
    this.preset.set('custom');
    this.sleeves.update((sleeves) => [
      ...sleeves,
      this.sleeve(
        `Під-портфель ${sleeves.length + 1}`,
        Math.max(0, 100 - this.total()),
        this.strategies()[0]?.id ?? null,
      ),
    ]);
  }

  protected remove(key: number): void {
    this.sleeves.update((sleeves) => sleeves.filter((sleeve) => sleeve.key !== key));
  }

  protected balance(): void {
    const sleeves = this.sleeves();
    if (!sleeves.length) return;

    const others = sleeves.slice(0, -1).reduce((sum, sleeve) => sum + sleeve.targetSharePercent, 0);

    this.update(sleeves.at(-1)!.key, {
      targetSharePercent: Math.max(1, Math.round((100 - others) * 100) / 100),
    });
  }

  protected useOwn(code: string, strategyTiming: ReportTiming, own: boolean): void {
    this.ownTimes.update((times) => {
      const { [code]: _, ...rest } = times;

      return own ? { ...rest, [code]: times[code] ?? strategyTiming } : rest;
    });
  }

  protected setOwn(code: string, timing: ReportTiming): void {
    this.ownTimes.update((times) => ({ ...times, [code]: timing }));
  }

  protected blurb(strategy: Strategy) {
    return describeStrategy(
      strategy,
      (id) => this.instruments.symbol(id),
      (id) => this.instruments.byId().get(id)?.tradingCurrency,
    );
  }

  protected async save(): Promise<void> {
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
      this.messages.add({
        severity: 'error',
        summary: 'Рахунок не створено',
        detail: toProblemMessage(error),
        life: 8000,
      });
    } finally {
      this.saving.set(false);
    }
  }

  private openAccount() {
    const plan = this.plan();

    return this.api.open({
      name: this.name().trim(),
      broker: this.broker().trim(),
      baseCurrency: this.currency(),
      taxPolicyCode: this.taxPolicy(),
      subPortfolios: this.sleeves().map((sleeve): SubPortfolioInput => ({
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
      generateOnSchedule: plan.generateOnSchedule,
    });
  }

  protected readonly createdId = signal<string | null>(null);

  private async saveOwnTimes(accountId: string): Promise<void> {
    for (const report of this.reports()) {
      if (!report.own) continue;

      try {
        await this.api.changeReportSchedule(accountId, report.code, report.own);
      } catch (error) {
        this.messages.add({
          severity: 'warn',
          summary: `Час рапорту «${report.name}» не збережено`,
          life: 10000,
          detail: `${toProblemMessage(error)} Рапорт приходитиме за часом стратегії; змінити можна на сторінці рахунку.`,
        });
      }
    }
  }

  protected async go(path: string[]): Promise<void> {
    await this.router.navigate(path, { replaceUrl: true });
  }

  private sleeve(name: string, share: number, strategyId: string | null): SleeveDraft {
    return { key: this.nextKey++, name, targetSharePercent: share, strategyId };
  }
}

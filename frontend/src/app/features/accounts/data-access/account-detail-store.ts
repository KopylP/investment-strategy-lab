import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { AccountSetup } from '../../../shared/models/api';
import { PlanSettingsVm } from '../ui/plan-settings-form/plan-settings-form';
import { AccountsApi, normalizeToHundred, NewSubPortfolio } from './accounts-api';

@Injectable()
export class AccountDetailStore {
  private readonly api = inject(AccountsApi);
  private readonly messages = inject(MessageService);
  private readonly context = inject(AccountContext);

  private readonly id = signal<string | undefined>(undefined);
  private readonly accountResource = resource({
    params: () => this.id(),
    loader: ({ params }) => this.api.get(params),
  });
  private readonly settingsResource = resource({
    params: () => this.id(),
    loader: ({ params }) => this.api.planSettings(params),
  });
  private readonly strategiesResource = resource({ loader: () => this.api.strategies() });

  readonly account = computed(() =>
    this.accountResource.hasValue() ? this.accountResource.value() : undefined,
  );
  readonly isLoading = this.accountResource.isLoading;
  readonly loadError = this.accountResource.error;
  readonly active = computed(
    () => this.account()?.subPortfolios.filter((sub) => sub.status === 'Active') ?? [],
  );
  readonly retired = computed(
    () => this.account()?.subPortfolios.filter((sub) => sub.status === 'Retired') ?? [],
  );
  readonly retiredNames = computed(() =>
    this.retired()
      .map((sub) => sub.name)
      .join(', '),
  );
  readonly strategies = computed(() =>
    (this.strategiesResource.value() ?? []).filter((strategy) => strategy.status === 'Active'),
  );
  readonly busy = signal<string | null>(null);

  readonly planSettings = computed<PlanSettingsVm | undefined>(() => {
    const settings = this.settingsResource.value();
    const account = this.account();
    if (!settings || !account) return undefined;

    return {
      monthlyContribution: settings.monthlyContribution,
      minTrade: settings.minTrade,
      trimBandPercent: Math.round(settings.trimBand * 10000) / 100,
      allowFractionalShares: settings.allowFractionalShares,
      generateOnSchedule: settings.generateOnSchedule,
      contributionPolicy: account.contributionPolicy,
      driftBandPercent: Math.round(account.driftBand * 10000) / 100,
    };
  });

  open(id: string): void {
    this.id.set(id);
    this.context.focus(id);
  }

  strategyName(id: string | null): string {
    return (id && this.strategies().find((strategy) => strategy.id === id)?.name) || '—';
  }

  changeTargetShares(sharesPercent: Record<string, number>): Promise<boolean> {
    return this.run(
      'shares',
      () => this.api.changeTargetShares(this.requireId(), sharesPercent),
      'Цільові частки змінено',
    );
  }

  assignStrategy(subPortfolioId: string, strategyId: string | null): Promise<boolean> {
    return this.run(
      `strategy-${subPortfolioId}`,
      () => this.api.assignStrategy(this.requireId(), subPortfolioId, strategyId, null),
      'Стратегію призначено',
    );
  }

  addSubPortfolio(input: NewSubPortfolio): Promise<boolean> {
    const remaining = 100 - input.targetSharePercent;
    const others: Record<string, number> = Object.fromEntries(
      this.active().map((sub) => [sub.id, Math.round(sub.targetShare * remaining * 100) / 100]),
    );
    const ids = Object.keys(others);
    if (ids.length) {
      const drift =
        Math.round((remaining - ids.reduce((sum, id) => sum + others[id], 0)) * 100) / 100;
      const largest = ids.reduce((best, id) => (others[id] > others[best] ? id : best), ids[0]);

      others[largest] = Math.round((others[largest] + drift) * 100) / 100;
    }

    return this.run(
      'add',
      () => this.api.addSubPortfolio(this.requireId(), input, others),
      'Під-портфель додано',
    );
  }

  retire(subPortfolioId: string): Promise<boolean> {
    const remaining = Object.fromEntries(
      this.active()
        .filter((sub) => sub.id !== subPortfolioId)
        .map((sub) => [sub.id, sub.targetShare * 100]),
    );

    return this.run(
      `retire-${subPortfolioId}`,
      () => this.api.retire(this.requireId(), subPortfolioId, normalizeToHundred(remaining)),
      'Під-портфель виведено',
    );
  }

  async savePlan(settings: PlanSettingsVm): Promise<boolean> {
    const id = this.requireId();

    this.busy.set('plan');

    try {
      this.accountResource.set(await this.persistPlan(id, settings));
      this.settingsResource.reload();
      this.messages.add({ severity: 'success', summary: 'План збережено' });

      return true;
    } catch (error) {
      this.reportPlanNotSaved(error);

      return false;
    } finally {
      this.busy.set(null);
    }
  }

  private async persistPlan(id: string, settings: PlanSettingsVm) {
    await this.api.changePlanSettings(id, {
      monthlyContribution: settings.monthlyContribution,
      minTrade: settings.minTrade,
      trimBand: settings.trimBandPercent / 100,
      allowFractionalShares: settings.allowFractionalShares,
      generateOnSchedule: settings.generateOnSchedule,
    });

    return this.api.changeContributionPolicy(
      id,
      settings.contributionPolicy,
      settings.driftBandPercent,
    );
  }

  private reportPlanNotSaved(error: unknown): void {
    this.messages.add({
      severity: 'error',
      summary: 'План не збережено',
      detail: toProblemMessage(error),
    });
  }

  async importPositions(subPortfolioId: string, json: unknown, asOf: string): Promise<boolean> {
    this.busy.set('import');

    try {
      await this.api.importPositions(this.requireId(), subPortfolioId, json, asOf);
      this.messages.add({
        severity: 'success',
        summary: 'Позиції імпортовано',
        detail: 'Повторний імпорт того самого файлу нічого не змінить',
      });

      return true;
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: 'Імпорт не вдався',
        detail: toProblemMessage(error),
        life: 10000,
      });

      return false;
    } finally {
      this.busy.set(null);
    }
  }

  private async run(
    key: string,
    command: () => Promise<AccountSetup>,
    success: string,
  ): Promise<boolean> {
    this.busy.set(key);

    try {
      this.accountResource.set(await command());
      this.context.reload();
      this.messages.add({ severity: 'success', summary: success });

      return true;
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: 'Не вдалося зберегти',
        detail: toProblemMessage(error),
        life: 8000,
      });

      return false;
    } finally {
      this.busy.set(null);
    }
  }

  private requireId(): string {
    const id = this.id();
    if (!id) throw new Error('Account is not opened');

    return id;
  }
}

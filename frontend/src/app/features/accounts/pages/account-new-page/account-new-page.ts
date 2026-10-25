import { ChangeDetectionStrategy, Component, computed, inject, resource, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { toProblemMessage } from '../../../../core/api/api-client';
import { AccountContext } from '../../../../core/context/account-context';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { symbolOf } from '../../../../shared/models/api';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { AccountsApi, SubPortfolioInput } from '../../data-access/accounts-api';
import { PlanSettingsForm, PlanSettingsVm } from '../../ui/plan-settings-form/plan-settings-form';

interface SleeveDraft {
  key: number;
  name: string;
  targetSharePercent: number;
  strategyId: string | null;
  cashInstrumentId: string | null;
  maxPositionWeightPercent: number | null;
}

@Component({
  selector: 'app-account-new-page',
  imports: [FormsModule, Button, InputNumber, InputText, Message, Select, SelectButton, TableModule, PageHeader, PlanSettingsForm],
  templateUrl: './account-new-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountNewPage {
  private readonly api = inject(AccountsApi);
  private readonly accounts = inject(AccountContext);
  private readonly messages = inject(MessageService);
  private readonly router = inject(Router);
  private readonly instruments = inject(InstrumentCatalog);
  private nextKey = 1;

  private readonly strategiesResource = resource({ loader: () => this.api.strategies() });

  protected readonly name = signal('Брокерський рахунок');
  protected readonly broker = signal('XTB');
  protected readonly currency = signal<'EUR' | 'USD'>('EUR');
  protected readonly sleeves = signal<SleeveDraft[]>([this.sleeve('Основний', 100)]);
  protected readonly plan = signal<PlanSettingsVm>({
    monthlyContribution: 227,
    minTrade: 25,
    trimBandPercent: 10,
    allowFractionalShares: true,
    generateOnSchedule: true,
    contributionPolicy: 'FillUnderweight',
    driftBandPercent: 5,
  });
  protected readonly saving = signal(false);
  protected readonly creatingDefaults = signal(false);

  protected readonly currencies = [{ label: 'EUR', value: 'EUR' }, { label: 'USD', value: 'USD' }];
  protected readonly strategies = computed(() => (this.strategiesResource.value() ?? []).filter((strategy) => strategy.status === 'Active'));
  protected readonly strategyOptions = computed(() => this.strategies().map((strategy) => ({ label: strategy.name, value: strategy.id })));
  protected readonly cashOptions = computed(() => this.instruments.etfs().map((etf) => ({ label: `${symbolOf(etf)} · ${etf.name}`, value: etf.id })));
  protected readonly total = computed(() => Math.round(this.sleeves().reduce((sum, sleeve) => sum + (sleeve.targetSharePercent ?? 0), 0) * 100) / 100);
  protected readonly valid = computed(() =>
    this.name().trim() && this.broker().trim() && this.total() === 100 && this.sleeves().every((sleeve) => sleeve.name.trim() && sleeve.targetSharePercent > 0),
  );

  protected update(key: number, change: Partial<SleeveDraft>): void {
    this.sleeves.update((sleeves) => sleeves.map((sleeve) => (sleeve.key === key ? { ...sleeve, ...change } : sleeve)));
  }

  protected add(): void {
    this.sleeves.update((sleeves) => [...sleeves, this.sleeve(`Під-портфель ${sleeves.length + 1}`, Math.max(0, 100 - this.total()))]);
  }

  protected remove(key: number): void {
    this.sleeves.update((sleeves) => sleeves.filter((sleeve) => sleeve.key !== key));
  }

  /** The Python flow's setup: EU momentum 70%, US momentum 30%. */
  protected async useDefaultSetup(): Promise<void> {
    this.creatingDefaults.set(true);
    try {
      if (this.strategies().length < 2) {
        await this.api.createDefaultStrategies();
        this.strategiesResource.reload();
        await new Promise<void>((resolve) => setTimeout(resolve, 300));
      }
      const all = (await this.api.strategies()).filter((strategy) => strategy.status === 'Active');
      const eu = all.find((strategy) => strategy.name.startsWith('EU')) ?? all[0];
      const us = all.find((strategy) => strategy.name.startsWith('US')) ?? all[1];
      this.sleeves.set([
        { ...this.sleeve('EU momentum', 70), strategyId: eu?.id ?? null },
        { ...this.sleeve('US momentum', 30), strategyId: us?.id ?? null },
      ]);
      this.strategiesResource.reload();
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося підготувати стратегії', detail: toProblemMessage(error) });
    } finally {
      this.creatingDefaults.set(false);
    }
  }

  protected async save(): Promise<void> {
    if (!this.valid()) return;
    this.saving.set(true);
    try {
      const plan = this.plan();
      const account = await this.api.open({
        name: this.name().trim(),
        broker: this.broker().trim(),
        baseCurrency: this.currency(),
        subPortfolios: this.sleeves().map(
          (sleeve): SubPortfolioInput => ({
            name: sleeve.name.trim(),
            targetSharePercent: sleeve.targetSharePercent,
            strategyId: sleeve.strategyId,
            cashInstrumentId: sleeve.cashInstrumentId,
            maxPositionWeightPercent: sleeve.maxPositionWeightPercent,
          }),
        ),
        contributionPolicy: { kind: plan.contributionPolicy, driftBandPercent: plan.driftBandPercent },
      });
      await this.api.changePlanSettings(account.id, {
        monthlyContribution: plan.monthlyContribution,
        minTrade: plan.minTrade,
        trimBand: plan.trimBandPercent / 100,
        allowFractionalShares: plan.allowFractionalShares,
        generateOnSchedule: plan.generateOnSchedule,
      });
      this.accounts.reload();
      this.accounts.choose(account.id);
      this.messages.add({ severity: 'success', summary: 'Рахунок відкрито', detail: 'Імпортуйте позиції або згенеруйте перший рапорт' });
      await this.router.navigate(['/accounts', account.id]);
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Рахунок не створено', detail: toProblemMessage(error), life: 8000 });
    } finally {
      this.saving.set(false);
    }
  }

  private sleeve(name: string, share: number): SleeveDraft {
    return { key: this.nextKey++, name, targetSharePercent: share, strategyId: null, cashInstrumentId: null, maxPositionWeightPercent: null };
  }
}

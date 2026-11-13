import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { SubPortfolioSetup } from '../../../../shared/models/account-setup-dto';
import { PercentPipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { AccountDetailStore } from '../../data-access/account-detail-store';
import { PlanSettingsForm, PlanSettingsVm } from '../../ui/plan-settings-form/plan-settings-form';

import { ReportScheduleCard } from '../../ui/report-schedule/report-schedule';
import { describeStrategy } from '../../../strategies/models/strategy-summary';
import { StrategyPicker } from '../../../strategies/ui/strategy-picker/strategy-picker';
import { Viewport } from '../../../../shared/ui/viewport';
import { AddSubPortfolioDialog } from '../../ui/add-sub-portfolio-dialog/add-sub-portfolio-dialog';

import { ImportPositionsDialog } from '../../ui/import-positions-dialog/import-positions-dialog';
import { RenameSubPortfolioDialog } from '../../ui/rename-sub-portfolio-dialog/rename-sub-portfolio-dialog';

@Component({
  selector: 'app-account-detail-page',
  imports: [
    TranslocoPipe,
    ImportPositionsDialog,
    AddSubPortfolioDialog,
    RenameSubPortfolioDialog,
    ReportScheduleCard,
    StrategyPicker,
    FormsModule,
    RouterLink,
    Button,
    Dialog,
    InputNumber,
    Message,
    Skeleton,
    TableModule,
    Tag,
    PageHeader,
    PlanSettingsForm,
    PercentPipe,
  ],
  providers: [AccountDetailStore],
  templateUrl: './account-detail-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountDetailPage {
  protected readonly addVisible = signal(false);
  protected readonly importVisible = signal(false);
  protected readonly store = inject(AccountDetailStore);
  protected readonly instruments = inject(InstrumentCatalog);
  private readonly confirm = inject(ConfirmationService);
  protected readonly viewport = inject(Viewport);

  readonly accountId = input.required<string>();

  protected readonly editingShares = signal(false);
  protected readonly shares = signal<Record<string, number>>({});
  protected readonly sharesTotal = computed(
    () =>
      Math.round(Object.values(this.shares()).reduce((sum, share) => sum + (share ?? 0), 0) * 100) /
      100,
  );

  protected readonly plan = linkedSignal<PlanSettingsVm | undefined>(() =>
    this.store.planSettings(),
  );

  protected readonly pickFor = signal<SubPortfolioSetup | null>(null);
  protected readonly renameFor = signal<SubPortfolioSetup | null>(null);
  constructor() {
    effect(() => this.store.open(this.accountId()));
  }

  protected startEditingShares(): void {
    this.shares.set(
      Object.fromEntries(
        this.store.active().map((sub) => [sub.id, Math.round(sub.targetShare * 10000) / 100]),
      ),
    );
    this.editingShares.set(true);
  }

  protected setShare(id: string, value: number | null): void {
    this.shares.update((shares) => ({ ...shares, [id]: value ?? 0 }));
  }

  protected async saveShares(): Promise<void> {
    if (await this.store.changeTargetShares(this.shares())) this.editingShares.set(false);
  }

  protected blurbOf(strategyId: string | null): string {
    const strategy = strategyId
      ? this.store.strategies().find((each) => each.id === strategyId)
      : undefined;
    if (!strategy) return translate('accounts.detail.noStrategyHint');

    return describeStrategy(
      strategy,
      (id) => this.instruments.symbol(id),
      (id) => this.instruments.byId().get(id)?.tradingCurrency,
    ).headline;
  }

  protected assign(sub: SubPortfolioSetup, strategyId: string | null): void {
    void this.store.assignStrategy(sub.id, strategyId);
  }

  protected confirmRetire(sub: SubPortfolioSetup): void {
    this.confirm.confirm({
      header: translate('accounts.detail.retireQuestion', { name: sub.name }),
      message: translate('accounts.detail.retireHint'),
      icon: 'pi pi-sign-out',
      acceptLabel: translate('accounts.detail.retire'),
      rejectLabel: translate('common.cancel'),
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.retire(sub.id),
    });
  }

  protected async savePlan(): Promise<void> {
    const plan = this.plan();
    if (plan) await this.store.savePlan(plan);
  }
}

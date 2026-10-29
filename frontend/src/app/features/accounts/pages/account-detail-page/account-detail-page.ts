import { ChangeDetectionStrategy, Component, computed, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { SubPortfolioSetup, symbolOf, toIsoDate } from '../../../../shared/models/api';
import { PercentPipe } from '../../../../shared/pipes/format-pipes';
import { FileDrop } from '../../../../shared/ui/file-drop';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { AccountDetailStore } from '../../data-access/account-detail-store';
import { PlanSettingsForm, PlanSettingsVm } from '../../ui/plan-settings-form/plan-settings-form';

interface SubPortfolioDraft {
  name: string;
  share: number;
  strategyId: string | null;
  cashInstrumentId: string | null;
}

import { ReportScheduleCard } from '../../ui/report-schedule/report-schedule';
import { describeStrategy } from '../../../strategies/models/strategy-summary';
import { StrategyPicker } from '../../../strategies/ui/strategy-picker/strategy-picker';
import { Viewport } from '../../../../shared/ui/viewport';
@Component({
  selector: 'app-account-detail-page',
  imports: [ReportScheduleCard, StrategyPicker,
    FormsModule, RouterLink, Button, DatePicker, Dialog, InputNumber, InputText, Message, Select, Skeleton, TableModule, Tag,
    PageHeader, PlanSettingsForm, FileDrop, PercentPipe,
  ],
  providers: [AccountDetailStore],
  templateUrl: './account-detail-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountDetailPage {
  protected readonly store = inject(AccountDetailStore);
  protected readonly instruments = inject(InstrumentCatalog);
  private readonly confirm = inject(ConfirmationService);
  protected readonly viewport = inject(Viewport);

  readonly accountId = input.required<string>();

  protected readonly editingShares = signal(false);
  protected readonly shares = signal<Record<string, number>>({});
  protected readonly sharesTotal = computed(() => Math.round(Object.values(this.shares()).reduce((sum, share) => sum + (share ?? 0), 0) * 100) / 100);

  protected readonly plan = linkedSignal<PlanSettingsVm | undefined>(() => this.store.planSettings());

  /** The sub-portfolio whose strategy is being chosen in the picker dialog. */
  protected readonly pickFor = signal<SubPortfolioSetup | null>(null);
  protected readonly cashOptions = computed(() => this.instruments.etfs().map((etf) => ({ label: `${symbolOf(etf)} · ${etf.name}`, value: etf.id })));

  protected readonly addVisible = signal(false);
  protected readonly draft = signal<SubPortfolioDraft>({ name: '', share: 10, strategyId: null, cashInstrumentId: null });

  protected readonly importVisible = signal(false);
  protected readonly importSleeve = signal<string | null>(null);
  protected readonly importDate = signal<Date>(new Date());
  protected readonly importJson = signal<unknown>(null);
  protected readonly importFileName = signal('');
  protected readonly importError = signal<string | null>(null);

  constructor() {
    effect(() => this.store.open(this.accountId()));
  }

  protected startEditingShares(): void {
    this.shares.set(Object.fromEntries(this.store.active().map((sub) => [sub.id, Math.round(sub.targetShare * 10000) / 100])));
    this.editingShares.set(true);
  }

  protected setShare(id: string, value: number | null): void {
    this.shares.update((shares) => ({ ...shares, [id]: value ?? 0 }));
  }

  protected async saveShares(): Promise<void> {
    if (await this.store.changeTargetShares(this.shares())) this.editingShares.set(false);
  }

  protected blurbOf(strategyId: string | null): string {
    const strategy = strategyId ? this.store.strategies().find((each) => each.id === strategyId) : undefined;
    if (!strategy) return 'Рапорти цей під-портфель не чіпають — оберіть стратегію';
    return describeStrategy(strategy, (id) => this.instruments.symbol(id), (id) => this.instruments.byId().get(id)?.tradingCurrency).headline;
  }

  protected assign(sub: SubPortfolioSetup, strategyId: string | null): void {
    void this.store.assignStrategy(sub.id, strategyId);
  }

  protected confirmRetire(sub: SubPortfolioSetup): void {
    this.confirm.confirm({
      header: `Вивести «${sub.name}»?`,
      message: 'Частку буде пропорційно розподілено між іншими під-портфелями. Позиції лишаються видимими, їх можна перенести внутрішнім переказом.',
      icon: 'pi pi-sign-out',
      acceptLabel: 'Вивести',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.retire(sub.id),
    });
  }

  protected openAdd(): void {
    this.draft.set({ name: '', share: 10, strategyId: null, cashInstrumentId: null });
    this.addVisible.set(true);
  }

  protected patchDraft(change: Partial<SubPortfolioDraft>): void {
    this.draft.update((draft) => ({ ...draft, ...change }));
  }

  protected async add(): Promise<void> {
    const draft = this.draft();
    const added = await this.store.addSubPortfolio({
      name: draft.name.trim(),
      targetSharePercent: draft.share,
      strategyId: draft.strategyId,
      cashInstrumentId: draft.cashInstrumentId,
      maxPositionWeightPercent: null,
    });
    if (added) this.addVisible.set(false);
  }

  protected async savePlan(): Promise<void> {
    const plan = this.plan();
    if (plan) await this.store.savePlan(plan);
  }

  protected openImport(): void {
    this.importSleeve.set(this.store.active()[0]?.id ?? null);
    this.importJson.set(null);
    this.importFileName.set('');
    this.importError.set(null);
    this.importVisible.set(true);
  }

  protected async readFile(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.importFileName.set(file.name);
    try {
      const json = JSON.parse(await file.text()) as { positions?: unknown[] };
      if (!Array.isArray(json.positions)) throw new Error('У файлі немає масиву positions');
      this.importJson.set(json);
      this.importError.set(null);
    } catch (error) {
      this.importJson.set(null);
      this.importError.set(error instanceof Error ? error.message : 'Не JSON');
    }
  }

  protected async runImport(): Promise<void> {
    const sleeve = this.importSleeve();
    const json = this.importJson();
    if (!sleeve || !json) return;
    if (await this.store.importPositions(sleeve, json, toIsoDate(this.importDate()))) this.importVisible.set(false);
  }
}

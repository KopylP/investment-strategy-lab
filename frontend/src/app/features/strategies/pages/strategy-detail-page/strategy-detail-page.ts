import { StrategyBacktest } from '../../../backtesting/ui/strategy-backtest/strategy-backtest';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { MultiSelect } from 'primeng/multiselect';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { DateTimePipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { JsonObject, SchemaForm } from '../../../../shared/ui/schema-form/schema-form';
import { scheduleLabel } from '../../../reports/models/report-schedule';
import { StrategyDetailStore } from '../../data-access/strategies-store';

@Component({
  selector: 'app-strategy-detail-page',
  imports: [
    FormsModule,
    Button,
    Message,
    MultiSelect,
    Select,
    Skeleton,
    Tag,
    PageHeader,
    SchemaForm,
    DateTimePipe,
    StrategyBacktest,
  ],
  providers: [StrategyDetailStore],
  templateUrl: './strategy-detail-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyDetailPage {
  protected readonly store = inject(StrategyDetailStore);
  private readonly confirm = inject(ConfirmationService);

  readonly strategyId = input.required<string>();

  protected readonly editing = signal(false);
  protected readonly parameters = signal<JsonObject>({});
  protected readonly universe = signal<string[]>([]);
  protected readonly cashInstrumentId = signal<string | null>(null);
  protected readonly benchmarkInstrumentId = signal<string | null>(null);
  protected readonly reports = computed(() =>
    (this.store.type()?.reports ?? []).map((report) => ({
      ...report,
      when: report.schedule ? scheduleLabel(report.schedule) : 'лише вручну',
    })),
  );

  protected readonly etfOptions = computed(() =>
    this.store.instruments.etfs().map((instrument) => ({
      label: `${instrument.label} · ${instrument.name}`,
      value: instrument.id,
      group: instrument.group ?? '',
    })),
  );
  protected readonly isLatest = computed(
    () => this.store.strategy()?.version.number === this.store.strategy()?.latestVersion,
  );

  constructor() {
    effect(() => this.store.open(this.strategyId()));
  }

  protected startEditing(): void {
    const version = this.store.strategy()?.version;
    if (!version) return;

    this.parameters.set(structuredClone(version.parameters));
    this.universe.set([...version.universe]);
    this.cashInstrumentId.set(version.cashInstrumentId);
    this.benchmarkInstrumentId.set(version.benchmarkInstrumentId);
    this.editing.set(true);
  }

  protected async save(): Promise<void> {
    const version = this.store.strategy()?.version;
    if (!version) return;

    const saved = await this.store.save({
      universe: this.universe(),
      benchmarkInstrumentId: this.benchmarkInstrumentId() ?? version.benchmarkInstrumentId,
      volatilityIndexInstrumentId: version.volatilityIndexInstrumentId,
      cashInstrumentId: this.cashInstrumentId(),
      parameters: this.parameters(),
    });
    if (saved) this.editing.set(false);
  }

  protected confirmArchive(): void {
    this.confirm.confirm({
      header: 'Архівувати стратегію?',
      message: 'Нові версії створювати не можна буде; рапорти, що на неї посилаються, лишаються.',
      icon: 'pi pi-box',
      acceptLabel: 'Архівувати',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.archive(),
    });
  }
}

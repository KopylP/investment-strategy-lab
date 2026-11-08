import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { KeyValuePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { DatePipe, MoneyPipe, PercentPipe, QuantityPipe } from '../../../../shared/pipes/format-pipes';
import { ExperimentStore } from '../../data-access/experiment-store';
import { activeLocale } from '../../../../core/i18n/language';

@Component({
  selector: 'app-experiment-journal',
  imports: [KeyValuePipe, FormsModule, Button, InputText, Message, Select, DatePipe, MoneyPipe, PercentPipe, QuantityPipe],
  templateUrl: './experiment-journal.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExperimentJournal {
  protected readonly store = inject(ExperimentStore);
  protected readonly currency = computed(() => this.store.experiment()?.result?.baseline.currency ?? '');
  protected readonly journalKinds = [
    { label: 'Усі записи', value: null },
    { label: 'Ребалансування', value: 'rebalance' },
    { label: 'Перевірка ризику', value: 'risk' },
    { label: 'Купівлі', value: 'buy' },
    { label: 'Продажі', value: 'sell' },
    { label: 'Прострочені накази', value: 'expired' },
  ];

  protected kindName(kind: string): string {
    return this.journalKinds.find((choice) => choice.value === kind)?.label ?? kind;
  }

  protected instrumentName(id: string): string {
    return this.store.experiment()?.result?.instrumentLabels?.[id] ?? id;
  }

  protected targetWeight(id: string, targets: { instrumentId: string; weight: number }[]): number | null {
    return targets.find((target) => target.instrumentId === id)?.weight ?? null;
  }

  protected shownValue(value: unknown): string {
    if (value === null || value === undefined) return '—';
    if (typeof value === 'number') return value.toLocaleString(activeLocale, { maximumFractionDigits: 6 });
    if (typeof value === 'boolean') return value ? 'Так' : 'Ні';

    return String(value);
  }
}

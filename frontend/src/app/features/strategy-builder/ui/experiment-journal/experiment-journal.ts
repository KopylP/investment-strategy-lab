import { TranslocoPipe, translate } from '@jsverse/transloco';
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
  imports: [TranslocoPipe, KeyValuePipe, FormsModule, Button, InputText, Message, Select, DatePipe, MoneyPipe, PercentPipe, QuantityPipe],
  templateUrl: './experiment-journal.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExperimentJournal {
  protected readonly store = inject(ExperimentStore);
  protected readonly currency = computed(() => this.store.experiment()?.result?.baseline.currency ?? '');
  protected readonly journalKinds = [
    { label: translate('builder.journal.kinds.all'), value: null },
    { label: translate('builder.journal.kinds.rebalance'), value: 'rebalance' },
    { label: translate('builder.journal.kinds.risk'), value: 'risk' },
    { label: translate('builder.journal.kinds.buys'), value: 'buy' },
    { label: translate('builder.journal.kinds.sells'), value: 'sell' },
    { label: translate('builder.journal.kinds.expired'), value: 'expired' },
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
    if (typeof value === 'boolean') return value ? translate('builder.inspector.gate.yes') : translate('builder.inspector.gate.no');

    return String(value);
  }
}

import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { MoneyPipe, QuantityPipe, DatePipe } from '../../../../shared/pipes/format-pipes';
import { ConfirmationService } from 'primeng/api';
import { RowDecision, StatementImport } from '../../data-access/statement-import-store';
import { StatementImportStore } from '../../data-access/statement-import-store';

const decisions: Record<
  RowDecision,
  { label: string; severity: 'success' | 'secondary' | 'warn' | 'info' | 'danger' | 'contrast' }
> = {
  New: { label: translate('transactions.rows.decisionLabel.New'), severity: 'success' },
  AlreadyImported: {
    label: translate('transactions.rows.decisionLabel.AlreadyImported'),
    severity: 'secondary',
  },
  Differs: { label: translate('transactions.rows.decisionLabel.Differs'), severity: 'warn' },
  MatchesExisting: {
    label: translate('transactions.rows.decisionLabel.MatchesExisting'),
    severity: 'info',
  },
  OutOfScope: {
    label: translate('transactions.rows.decisionLabel.OutOfScope'),
    severity: 'secondary',
  },
  Unsupported: {
    label: translate('transactions.rows.decisionLabel.Unsupported'),
    severity: 'contrast',
  },
  NeedsMapping: {
    label: translate('transactions.rows.decisionLabel.NeedsMapping'),
    severity: 'danger',
  },
};

const kinds: Record<string, string> = {
  Buy: translate('chart.buy'),
  Sell: translate('chart.sell'),
  Contribution: translate('transactions.rows.kind.Contribution'),
  Withdrawal: translate('transactions.cash.kind.Withdrawal'),
  Dividend: translate('transactions.cash.kind.Dividend'),
  Unsupported: translate('transactions.rows.kind.Unsupported'),
};

@Component({
  selector: 'app-statement-rows-card',
  imports: [
    TranslocoPipe,
    FormsModule,
    RouterLink,
    Button,
    Message,
    TableModule,
    Tag,
    ToggleSwitch,
    MoneyPipe,
    QuantityPipe,
    DatePipe,
  ],
  templateUrl: './statement-rows-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatementRowsCard {
  readonly current = input.required<StatementImport>();
  readonly undo = output<string>();
  protected readonly store = inject(StatementImportStore);

  private readonly confirmation = inject(ConfirmationService);

  protected readonly onlyChanges = signal(false);

  protected readonly rows = computed(() => {
    const rows = this.store.current()?.rows ?? [];

    return (
      this.onlyChanges()
        ? rows.filter(
            (row) => row.willRecord || row.willReverseExisting || row.decision === 'NeedsMapping',
          )
        : rows
    ).map((row) => ({
      ...row,
      kindLabel: kinds[row.kind] ?? row.rawType,
      decisionLabel: decisions[row.decision].label,
      decisionSeverity: decisions[row.decision].severity,
    }));
  });

  protected readonly blockers = computed(() => {
    const current = this.store.current();
    if (!current || this.store.canConfirm()) return null;

    const unmapped = current.rows.filter((row) => row.decision === 'NeedsMapping');
    if (unmapped.length === 0) return [translate('transactions.rows.nothingNew')];

    const reasons = new Map<string, number>();

    for (const row of unmapped) {
      const reason = row.note ?? translate<string>('transactions.rows.unmapped');

      reasons.set(reason, (reasons.get(reason) ?? 0) + 1);
    }

    return [
      translate('transactions.rows.unmappedRows', { count: unmapped.length }),
      ...[...reasons].map(([reason, count]) =>
        translate('transactions.rows.reasonRows', { reason, count }),
      ),
    ];
  });

  protected confirm(): void {
    const current = this.store.current();
    if (!current) return;

    const dangerous = current.mode === 'Replace' || current.mode === 'FromScratch';
    const toReverse = current.counts.toReverse;
    const message = toReverse
      ? translate('transactions.rows.confirmMessageWithReversals', {
          count: current.counts.toRecord,
          reversed: toReverse,
        })
      : translate('transactions.rows.confirmMessage', { count: current.counts.toRecord });

    this.confirmation.confirm({
      header: dangerous
        ? translate('transactions.rows.reverseAndImportQuestion')
        : translate('transactions.rows.importQuestion'),
      message,
      icon: dangerous ? 'pi pi-exclamation-triangle' : 'pi pi-upload',
      acceptLabel: translate('transactions.rows.importAction'),
      rejectLabel: translate('common.cancel'),
      acceptButtonProps: dangerous ? { severity: 'danger' } : undefined,
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.confirm(),
    });
  }
}

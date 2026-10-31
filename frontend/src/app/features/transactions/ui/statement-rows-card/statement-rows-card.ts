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
  New: { label: 'нова', severity: 'success' },
  AlreadyImported: { label: 'вже є', severity: 'secondary' },
  Differs: { label: 'відрізняється', severity: 'warn' },
  MatchesExisting: { label: 'збіг з журналом', severity: 'info' },
  OutOfScope: { label: 'до дати', severity: 'secondary' },
  Unsupported: { label: 'не імпортується', severity: 'contrast' },
  NeedsMapping: { label: 'потрібне зіставлення', severity: 'danger' },
};

const kinds: Record<string, string> = {
  Buy: 'Купівля',
  Sell: 'Продаж',
  Contribution: 'Поповнення',
  Withdrawal: 'Вивід',
  Dividend: 'Дивіденд',
  Unsupported: 'Інше',
};

@Component({
  selector: 'app-statement-rows-card',
  imports: [
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
    if (unmapped.length === 0) return ['У виписці немає нових операцій: усе вже в журналі.'];

    const reasons = new Map<string, number>();

    for (const row of unmapped)
      reasons.set(row.note ?? 'не зіставлено', (reasons.get(row.note ?? 'не зіставлено') ?? 0) + 1);

    return [
      `Рядків без зіставлення: ${unmapped.length}.`,
      ...[...reasons].map(([reason, count]) => `${reason} (рядків: ${count})`),
    ];
  });

  protected confirm(): void {
    const current = this.store.current();
    if (!current) return;

    const dangerous = current.mode === 'Replace' || current.mode === 'FromScratch';
    const toReverse = current.counts.toReverse;
    const reversals = toReverse ? `, сторновано ${toReverse}` : '';

    this.confirmation.confirm({
      header: dangerous ? 'Сторнувати й імпортувати заново?' : 'Імпортувати виписку?',
      message: `Буде записано ${current.counts.toRecord} операцій${reversals}. Імпорт можна скасувати одним кроком.`,
      icon: dangerous ? 'pi pi-exclamation-triangle' : 'pi pi-upload',
      acceptLabel: 'Імпортувати',
      rejectLabel: 'Скасувати',
      acceptButtonProps: dangerous ? { severity: 'danger' } : undefined,
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.confirm(),
    });
  }
}

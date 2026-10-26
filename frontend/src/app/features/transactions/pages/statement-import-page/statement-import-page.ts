import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { fromIsoDate, symbolOf, toIsoDate } from '../../../../shared/models/api';
import { DatePipe, DateTimePipe, MoneyPipe, QuantityPipe } from '../../../../shared/pipes/format-pipes';
import { FileDrop } from '../../../../shared/ui/file-drop';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { ImportMode, RowDecision, StatementImportStore, TickerMapping } from '../../data-access/statement-import-store';
import { Viewport } from '../../../../shared/ui/viewport';

const newInstrument = '__new';

const decisions: Record<RowDecision, { label: string; severity: 'success' | 'secondary' | 'warn' | 'info' | 'danger' | 'contrast' }> = {
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
  selector: 'app-statement-import-page',
  imports: [
    FormsModule, RouterLink, Button, DatePicker, InputText, Message, Select, SelectButton, TableModule, Tag, ToggleSwitch, PageHeader, FileDrop,
    MoneyPipe, QuantityPipe, DatePipe, DateTimePipe,
  ],
  providers: [StatementImportStore],
  templateUrl: './statement-import-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatementImportPage {
  protected readonly store = inject(StatementImportStore);
  protected readonly viewport = inject(Viewport);
  private readonly catalog = inject(InstrumentCatalog);
  private readonly confirmation = inject(ConfirmationService);

  protected readonly onlyChanges = signal(false);

  /** `?importId=` opens an upload started elsewhere (the report's "load operations"). */
  readonly importId = input<string>();

  constructor() {
    effect(() => {
      const importId = this.importId();
      if (importId) void this.store.open(importId);
    });
  }

  protected readonly modes: { label: string; value: ImportMode }[] = [
    { label: 'Доповнити', value: 'Merge' },
    { label: 'Оновити імпортоване', value: 'UpdateImported' },
    { label: 'Перезаписати', value: 'Replace' },
    { label: 'З нуля', value: 'FromScratch' },
  ];

  protected readonly modeHelp: Record<ImportMode, { severity: 'info' | 'warn' | 'error'; text: string }> = {
    Merge: { severity: 'info', text: 'Додаються тільки нові рядки. Повторне завантаження тієї самої чи свіжішої виписки нічого не дублює.' },
    UpdateImported: { severity: 'info', text: 'Як «Доповнити», а рядки, імпортовані раніше з іншими цифрами, перезаписуються за випискою (сторно + новий запис).' },
    Replace: { severity: 'warn', text: 'Усі попередні імпорти (виписки й positions.json) буде сторновано, і виписку імпортовано заново. Ручні операції й відмітки з рапортів лишаються.' },
    FromScratch: { severity: 'error', text: 'Увесь журнал рахунку буде сторновано, потім імпортовано виписку. Використовуйте, якщо починаєте облік заново.' },
  };

  protected readonly instrumentOptions = computed(() => [
    { label: '➕ Новий інструмент (додати в каталог)', value: newInstrument },
    ...this.catalog.instruments().filter((instrument) => instrument.kind !== 'Index' && instrument.kind !== 'FxPair')
      .map((instrument) => ({ label: `${symbolOf(instrument)} · ${instrument.name}`, value: instrument.id })),
  ]);

  protected readonly rows = computed(() => {
    const rows = this.store.current()?.rows ?? [];
    return (this.onlyChanges() ? rows.filter((row) => row.willRecord || row.willReverseExisting || row.decision === 'NeedsMapping') : rows).map((row) => ({
      ...row,
      kindLabel: kinds[row.kind] ?? row.rawType,
      decisionLabel: decisions[row.decision].label,
      decisionSeverity: decisions[row.decision].severity,
    }));
  });

  /** Why "Імпортувати" is disabled, in words; null when it is not. */
  protected readonly blockers = computed(() => {
    const current = this.store.current();
    if (!current || this.store.canConfirm()) return null;
    const unmapped = current.rows.filter((row) => row.decision === 'NeedsMapping');
    if (unmapped.length === 0) return ['У виписці немає нових операцій: усе вже в журналі.'];
    const reasons = new Map<string, number>();
    for (const row of unmapped) reasons.set(row.note ?? 'не зіставлено', (reasons.get(row.note ?? 'не зіставлено') ?? 0) + 1);
    return [`Рядків без зіставлення: ${unmapped.length}.`, ...[...reasons].map(([reason, count]) => `${reason} (рядків: ${count})`)];
  });

  protected instrumentValue(mapping: TickerMapping): string | null {
    return mapping.instrumentId ?? (mapping.registerAs !== null ? newInstrument : null);
  }

  protected async chooseInstrument(mapping: TickerMapping, value: string | null): Promise<void> {
    if (value === newInstrument)
      await this.store.configure({ mapping: { ticker: mapping.ticker, instrumentId: null, registerAs: mapping.registerAs ?? this.suggestSymbol(mapping) } });
    else await this.store.configure({ mapping: { ticker: mapping.ticker, instrumentId: value, registerAs: null } });
  }

  protected async setSymbol(mapping: TickerMapping, symbol: string): Promise<void> {
    await this.store.configure({ mapping: { ticker: mapping.ticker, instrumentId: null, registerAs: symbol.trim().toUpperCase() } });
  }

  protected async chooseSub(mapping: TickerMapping, subPortfolioId: string | null): Promise<void> {
    await this.store.configure({ mapping: { ticker: mapping.ticker, subPortfolioId } });
  }

  protected async readFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) await this.store.upload(file);
    input.value = '';
  }

  protected fromDate(): Date | null {
    const value = this.store.current()?.fromDate;
    return value ? fromIsoDate(value) : null;
  }

  protected async setFromDate(date: Date | null): Promise<void> {
    await this.store.configure({ fromDate: date ? toIsoDate(date) : null });
  }

  protected confirm(): void {
    const current = this.store.current();
    if (!current) return;
    const dangerous = current.mode === 'Replace' || current.mode === 'FromScratch';
    this.confirmation.confirm({
      header: dangerous ? 'Сторнувати й імпортувати заново?' : 'Імпортувати виписку?',
      message: `Буде записано ${current.counts.toRecord} операцій${current.counts.toReverse ? `, сторновано ${current.counts.toReverse}` : ''}. Імпорт можна скасувати одним кроком.`,
      icon: dangerous ? 'pi pi-exclamation-triangle' : 'pi pi-upload',
      acceptLabel: 'Імпортувати',
      rejectLabel: 'Ні',
      acceptButtonProps: dangerous ? { severity: 'danger' } : undefined,
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.confirm(),
    });
  }

  protected confirmUndo(importId: string): void {
    this.confirmation.confirm({
      header: 'Скасувати імпорт?',
      message: 'Усі операції, записані цим імпортом, буде сторновано.',
      icon: 'pi pi-undo',
      acceptLabel: 'Скасувати імпорт',
      rejectLabel: 'Ні',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.undo(importId),
    });
  }

  private suggestSymbol(mapping: TickerMapping): string {
    return mapping.currency === 'EUR' ? `${mapping.ticker}.DE` : mapping.ticker;
  }
}

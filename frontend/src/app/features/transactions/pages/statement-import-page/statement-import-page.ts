import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { DatePicker } from 'primeng/datepicker';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { fromIsoDate, symbolOf, toIsoDate } from '../../../../shared/models/api';
import {} from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import {
  ImportMode,
  StatementImportStore,
  TickerMapping,
} from '../../data-access/statement-import-store';
import { Viewport } from '../../../../shared/ui/viewport';
import { StatementUploadCard } from '../../ui/statement-upload-card/statement-upload-card';
import { StatementHistoryCard } from '../../ui/statement-history-card/statement-history-card';
import { StatementRowsCard } from '../../ui/statement-rows-card/statement-rows-card';

const newInstrument = '__new';

@Component({
  selector: 'app-statement-import-page',
  imports: [
    StatementRowsCard,
    StatementHistoryCard,
    StatementUploadCard,
    FormsModule,
    DatePicker,
    InputText,
    Message,
    Select,
    SelectButton,
    TableModule,
    PageHeader,
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

  protected readonly modeHelp: Record<
    ImportMode,
    { severity: 'info' | 'warn' | 'error'; text: string }
  > = {
    Merge: {
      severity: 'info',
      text: 'Додаються тільки нові рядки. Повторне завантаження тієї самої чи свіжішої виписки нічого не дублює.',
    },
    UpdateImported: {
      severity: 'info',
      text: 'Як «Доповнити», а рядки, імпортовані раніше з іншими цифрами, перезаписуються за випискою (сторно + новий запис).',
    },
    Replace: {
      severity: 'warn',
      text:
        'Усі попередні імпорти (виписки й positions.json) буде сторновано, і виписку імпортовано заново. ' +
        'Ручні операції й відмітки з рапортів лишаються.',
    },
    FromScratch: {
      severity: 'error',
      text: 'Увесь журнал рахунку буде сторновано, потім імпортовано виписку. Використовуйте, якщо починаєте облік заново.',
    },
  };

  protected readonly instrumentOptions = computed(() => [
    { label: '➕ Новий інструмент (додати в каталог)', value: newInstrument },
    ...this.catalog
      .instruments()
      .filter((instrument) => instrument.kind !== 'Index' && instrument.kind !== 'FxPair')
      .map((instrument) => ({
        label: `${symbolOf(instrument)} · ${instrument.name}`,
        value: instrument.id,
      })),
  ]);

  protected instrumentValue(mapping: TickerMapping): string | null {
    return mapping.instrumentId ?? (mapping.registerAs !== null ? newInstrument : null);
  }

  protected async chooseInstrument(mapping: TickerMapping, value: string | null): Promise<void> {
    if (value === newInstrument)
      await this.store.configure({
        mapping: {
          ticker: mapping.ticker,
          instrumentId: null,
          registerAs: mapping.registerAs ?? this.suggestSymbol(mapping),
        },
      });
    else
      await this.store.configure({
        mapping: { ticker: mapping.ticker, instrumentId: value, registerAs: null },
      });
  }

  protected async setSymbol(mapping: TickerMapping, symbol: string): Promise<void> {
    await this.store.configure({
      mapping: {
        ticker: mapping.ticker,
        instrumentId: null,
        registerAs: symbol.trim().toUpperCase(),
      },
    });
  }

  protected async chooseSub(mapping: TickerMapping, subPortfolioId: string | null): Promise<void> {
    await this.store.configure({ mapping: { ticker: mapping.ticker, subPortfolioId } });
  }

  protected fromDate(): Date | null {
    const value = this.store.current()?.fromDate;

    return value ? fromIsoDate(value) : null;
  }

  protected async setFromDate(date: Date | null): Promise<void> {
    await this.store.configure({ fromDate: date ? toIsoDate(date) : null });
  }

  protected confirmUndo(importId: string): void {
    this.confirmation.confirm({
      header: 'Скасувати імпорт?',
      message: 'Усі операції, записані цим імпортом, буде сторновано.',
      icon: 'pi pi-undo',
      acceptLabel: 'Скасувати імпорт',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.undo(importId),
    });
  }

  private suggestSymbol(mapping: TickerMapping): string {
    return mapping.currency === 'EUR' ? `${mapping.ticker}.DE` : mapping.ticker;
  }
}

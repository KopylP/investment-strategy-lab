import { TranslocoPipe, translate } from '@jsverse/transloco';
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
import { fromIsoDate, toIsoDate } from '../../../../shared/models/iso-date';
import {} from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { StatementImportStore } from '../../data-access/statement-import-store';
import { ImportMode, TickerMapping } from '../../models/statement-import-dto';
import { Viewport } from '../../../../shared/ui/viewport';
import { StatementUploadCard } from '../../ui/statement-upload-card/statement-upload-card';
import { StatementHistoryCard } from '../../ui/statement-history-card/statement-history-card';
import { StatementRowsCard } from '../../ui/statement-rows-card/statement-rows-card';

const newInstrument = '__new';

@Component({
  selector: 'app-statement-import-page',
  imports: [
    TranslocoPipe,
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
    { label: translate('transactions.import.mode.Append'), value: 'Merge' },
    { label: translate('transactions.import.mode.Update'), value: 'UpdateImported' },
    { label: translate('transactions.import.mode.Overwrite'), value: 'Replace' },
    { label: translate('transactions.import.mode.FromScratch'), value: 'FromScratch' },
  ];

  protected readonly modeHelp: Record<
    ImportMode,
    { severity: 'info' | 'warn' | 'error'; text: string }
  > = {
    Merge: {
      severity: 'info',
      text: translate('transactions.import.mode.AppendHint'),
    },
    UpdateImported: {
      severity: 'info',
      text: translate('transactions.import.mode.UpdateHint'),
    },
    Replace: {
      severity: 'warn',
      text: translate('transactions.import.mode.OverwriteHint'),
    },
    FromScratch: {
      severity: 'error',
      text: translate('transactions.import.mode.FromScratchHint'),
    },
  };

  protected readonly instrumentOptions = computed(() => [
    { label: translate('transactions.import.newInstrument'), value: newInstrument },
    ...this.catalog
      .instruments()
      .filter((instrument) => instrument.kind !== 'Index' && instrument.kind !== 'FxPair')
      .map((instrument) => ({
        label: `${instrument.label} · ${instrument.name}`,
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
      header: translate('transactions.import.cancelQuestion'),
      message: translate('transactions.import.cancelHint'),
      icon: 'pi pi-undo',
      acceptLabel: translate('transactions.history.cancelImport'),
      rejectLabel: translate('common.cancel'),
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.undo(importId),
    });
  }

  private suggestSymbol(mapping: TickerMapping): string {
    return mapping.currency === 'EUR' ? `${mapping.ticker}.DE` : mapping.ticker;
  }
}

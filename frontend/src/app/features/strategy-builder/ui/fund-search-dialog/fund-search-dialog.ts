import { TranslocoPipe } from '@jsverse/transloco';
import { DecimalPipe, PercentPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { FundSearchStore } from '../../data-access/fund-search-store';

@Component({
  selector: 'app-builder-fund-search-dialog',
  imports: [
    TranslocoPipe,
    DecimalPipe,
    PercentPipe,
    FormsModule,
    Button,
    Dialog,
    InputNumber,
    InputText,
    Message,
    Select,
    TableModule,
  ],
  providers: [FundSearchStore],
  templateUrl: './fund-search-dialog.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FundSearchDialog {
  protected readonly store = inject(FundSearchStore);
  readonly visible = model(false);
  readonly overlayContainer = input<HTMLElement | 'body'>('body');
  readonly selectedInstrumentIds = input.required<string[]>();
  readonly added = output<string[]>();
  protected readonly exchangeId = signal<string | null>(null);
  protected readonly regionId = signal<string | null>(null);
  protected readonly family = signal('');
  protected readonly minAssets = signal<number | null>(null);
  protected readonly maxFeePercent = signal<number | null>(null);
  protected readonly words = signal('');

  constructor() {
    effect(() => {
      if (this.visible()) untracked(() => void this.store.loadOptions());
    });
  }

  protected search(): void {
    const fee = this.maxFeePercent();
    const nameWords = this.words().split(/\s+/u).filter(Boolean);

    void this.store.search({
      exchangeId: this.exchangeId(),
      regionId: this.regionId(),
      fundFamily: this.family().trim() || null,
      minAssets: this.minAssets(),
      maxExpenseRatio: fee === null ? null : fee / 100,
      nameWords,
    });
  }

  protected async add(): Promise<void> {
    const result = await this.store.add(this.selectedInstrumentIds());
    if (!result) return;

    if (result.instrumentIds.length > 0) this.added.emit(result.instrumentIds);
    if (result.failed.length === 0) this.visible.set(false);
  }
}

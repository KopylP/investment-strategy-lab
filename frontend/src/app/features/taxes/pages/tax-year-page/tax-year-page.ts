import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { DatePipe, DateTimePipe, MoneyPipe, QuantityPipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { LossCarryforward, TaxesStore } from '../../data-access/taxes-store';

@Component({
  selector: 'app-tax-year-page',
  imports: [
    FormsModule, Button, InputNumber, Message, SelectButton, Skeleton, TableModule, Tag, PageHeader, EmptyState, SignedValue,
    MoneyPipe, QuantityPipe, DatePipe, DateTimePipe,
  ],
  providers: [TaxesStore],
  templateUrl: './tax-year-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxYearPage {
  protected readonly store = inject(TaxesStore);
  private readonly confirm = inject(ConfirmationService);

  protected readonly yearOptions = computed(() => this.store.years().map((year) => ({ label: String(year.year), value: year.year })));
  protected readonly lossYear = signal<number | null>(new Date().getFullYear() - 1);
  protected readonly lossAmount = signal<number | null>(null);
  protected readonly lossUsed = signal<number | null>(0);

  protected async addLoss(): Promise<void> {
    const year = this.lossYear();
    const amount = this.lossAmount();
    if (!year || !amount) return;
    if (await this.store.addLoss(year, amount, this.lossUsed() ?? 0)) this.lossAmount.set(null);
  }

  protected confirmRemove(loss: LossCarryforward): void {
    this.confirm.confirm({
      header: 'Видалити збиток?',
      message: `Збиток ${loss.year} року більше не зменшуватиме податок.`,
      acceptLabel: 'Видалити',
      rejectLabel: 'Ні',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.removeLoss(loss),
    });
  }
}

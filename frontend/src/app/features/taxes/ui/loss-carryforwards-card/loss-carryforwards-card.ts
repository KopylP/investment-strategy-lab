import { TranslocoPipe, translate } from '@jsverse/transloco';
import { TaxRegime } from '../../../../core/context/tax-regime';
import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { ConfirmationService } from 'primeng/api';
import { LossCarryforward } from '../../data-access/taxes-store';
import { TaxesStore } from '../../data-access/taxes-store';

@Component({
  selector: 'app-loss-carryforwards-card',
  imports: [TranslocoPipe, TaxMoneyPipe, FormsModule, Button, InputNumber, TableModule, MoneyPipe],
  templateUrl: './loss-carryforwards-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LossCarryforwardsCard {
  protected readonly store = inject(TaxesStore);
  protected readonly regime = inject(TaxRegime);

  private readonly confirm = inject(ConfirmationService);

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
      header: translate('taxes.losses.deleteQuestion'),
      message: translate('taxes.losses.deleteHint', { year: loss.year }),
      acceptLabel: translate('common.delete'),
      rejectLabel: translate('common.cancel'),
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.removeLoss(loss),
    });
  }
}

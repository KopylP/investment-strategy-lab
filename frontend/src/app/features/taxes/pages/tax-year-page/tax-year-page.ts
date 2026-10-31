import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import {
  DatePipe,
  DateTimePipe,
  MoneyPipe,
  QuantityPipe,
} from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { FileDrop } from '../../../../shared/ui/file-drop';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { DeclarationDialog } from '../../ui/declaration-dialog/declaration-dialog';
import {
  BrokerStatement,
  LossCarryforward,
  Realization,
  TaxesStore,
} from '../../data-access/taxes-store';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';

@Component({
  selector: 'app-tax-year-page',
  imports: [
    FormsModule,
    Button,
    InputNumber,
    Message,
    Select,
    SelectButton,
    Skeleton,
    TableModule,
    Tag,
    Tooltip,
    TapTooltip,
    PageHeader,
    EmptyState,
    SignedValue,
    DeclarationDialog,
    FileDrop,
    MoneyPipe,
    QuantityPipe,
    DatePipe,
    DateTimePipe,
  ],
  providers: [TaxesStore],
  templateUrl: './tax-year-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxYearPage {
  protected readonly store = inject(TaxesStore);
  private readonly confirm = inject(ConfirmationService);

  protected readonly yearOptions = computed(() =>
    this.store.years().map((year) => ({ label: String(year.year), value: year.year })),
  );
  protected readonly lossYear = signal<number | null>(new Date().getFullYear() - 1);
  protected readonly lossAmount = signal<number | null>(null);
  protected readonly lossUsed = signal<number | null>(0);

  protected async addLoss(): Promise<void> {
    const year = this.lossYear();
    const amount = this.lossAmount();
    if (!year || !amount) return;

    if (await this.store.addLoss(year, amount, this.lossUsed() ?? 0)) this.lossAmount.set(null);
  }

  protected brokerDifference(sale: Realization): boolean {
    return sale.brokerGainPln !== null && Math.abs(sale.gainPln - sale.brokerGainPln) >= 1;
  }

  protected async uploadBroker(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (file) await this.store.uploadBrokerStatement(file);
  }

  protected confirmRemoveBroker(statement: BrokerStatement): void {
    this.confirm.confirm({
      header: 'Видалити звіт брокера?',
      message:
        'Податок з дивідендів знову рахуватиметься без зарахування утриманого за кордоном, а собівартість зі звіту більше не братиметься.',
      acceptLabel: 'Видалити',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.removeBrokerStatement(statement),
    });
  }

  protected confirmRemovePit8C(accountId: string): void {
    this.confirm.confirm({
      header: 'Прибрати суми PIT-8C?',
      message:
        'Виручка й витрати брокера в декларації знову рахуватимуться з ваших операцій, а не з його PIT-8C.',
      acceptLabel: 'Прибрати',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.removePit8C(accountId),
    });
  }

  protected confirmRemove(loss: LossCarryforward): void {
    this.confirm.confirm({
      header: 'Видалити збиток?',
      message: `Збиток ${loss.year} року більше не зменшуватиме податок.`,
      acceptLabel: 'Видалити',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.removeLoss(loss),
    });
  }
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { FileDrop } from '../../../../shared/ui/file-drop';
import { DatePipe, DateTimePipe } from '../../../../shared/pipes/format-pipes';
import { ConfirmationService } from 'primeng/api';
import { BrokerStatement } from '../../data-access/taxes-store';
import { TaxesStore } from '../../data-access/taxes-store';

@Component({
  selector: 'app-broker-statements-card',
  imports: [FormsModule, Button, Select, Tag, FileDrop, DatePipe, DateTimePipe],
  templateUrl: './broker-statements-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrokerStatementsCard {
  protected readonly store = inject(TaxesStore);

  private readonly confirm = inject(ConfirmationService);

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
}

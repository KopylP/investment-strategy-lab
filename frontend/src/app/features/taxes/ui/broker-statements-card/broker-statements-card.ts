import { TranslocoPipe, translate } from '@jsverse/transloco';
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
  imports: [TranslocoPipe, FormsModule, Button, Select, Tag, FileDrop, DatePipe, DateTimePipe],
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
      header: translate('taxes.brokerStatements.deleteQuestion'),
      message:
        translate('taxes.brokerStatements.deleteHint'),
      acceptLabel: translate('common.delete'),
      rejectLabel: translate('common.cancel'),
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.removeBrokerStatement(statement),
    });
  }
}

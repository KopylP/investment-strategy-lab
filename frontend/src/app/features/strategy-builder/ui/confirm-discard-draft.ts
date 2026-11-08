import { translate } from '@jsverse/transloco';
import { ConfirmationService } from 'primeng/api';

export const publishedStrategiesNotice = translate('builder.discard.hint');

export function confirmDiscardDraft(
  confirmation: ConfirmationService,
  name: string,
  accept: () => void,
): void {
  confirmation.confirm({
    header: translate('builder.discard.question'),
    message: translate('builder.discard.message', { name, notice: publishedStrategiesNotice }),
    icon: 'pi pi-trash',
    acceptLabel: translate('common.delete'),
    rejectLabel: translate('common.cancel'),
    acceptButtonProps: { severity: 'danger' },
    rejectButtonProps: { severity: 'secondary', text: true },
    defaultFocus: 'reject',
    accept,
  });
}

import { ConfirmationService } from 'primeng/api';

export const publishedStrategiesNotice =
  'Опублікованими стратегіями можна керувати у вкладці «Стратегії». Видалення чернетки не впливає на них.';

export function confirmDiscardDraft(
  confirmation: ConfirmationService,
  name: string,
  accept: () => void,
): void {
  confirmation.confirm({
    header: 'Видалити чернетку?',
    message: `Чернетку «${name}» та її схему буде повністю видалено без можливості відновлення. ${publishedStrategiesNotice}`,
    icon: 'pi pi-trash',
    acceptLabel: 'Видалити',
    rejectLabel: 'Скасувати',
    acceptButtonProps: { severity: 'danger' },
    rejectButtonProps: { severity: 'secondary', text: true },
    defaultFocus: 'reject',
    accept,
  });
}

export type SaveStatus = 'saved' | 'saving' | 'failed';

export const saveStatuses: Record<SaveStatus, { icon: string; text: string }> = {
  saved: { icon: 'pi pi-cloud', text: 'Збережено' },
  saving: { icon: 'pi pi-spin pi-spinner', text: 'Зберігаю…' },
  failed: { icon: 'pi pi-exclamation-triangle', text: 'Не збережено' },
};

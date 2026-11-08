import { translate } from '@jsverse/transloco';
export type SaveStatus = 'saved' | 'saving' | 'failed';

export const saveStatuses: Record<SaveStatus, { icon: string; text: string }> = {
  saved: { icon: 'pi pi-cloud', text: translate('builder.save.saved') },
  saving: { icon: 'pi pi-spin pi-spinner', text: translate('builder.save.saving') },
  failed: { icon: 'pi pi-exclamation-triangle', text: translate('builder.save.notSaved') },
};

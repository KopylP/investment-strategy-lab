import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NewAccountDraft } from '../../data-access/new-account-draft';

@Component({
  selector: 'app-new-account-step-done',
  imports: [],
  templateUrl: './new-account-step-done.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewAccountStepDone {
  protected readonly draft = inject(NewAccountDraft);
}

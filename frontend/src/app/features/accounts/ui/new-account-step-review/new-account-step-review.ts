import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { NewAccountDraft } from '../../data-access/new-account-draft';

@Component({
  selector: 'app-new-account-step-review',
  imports: [Button, Message],
  templateUrl: './new-account-step-review.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewAccountStepReview {
  protected readonly draft = inject(NewAccountDraft);
}

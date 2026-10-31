import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { PlanSettingsForm } from '../plan-settings-form/plan-settings-form';
import { NewAccountDraft } from '../../data-access/new-account-draft';

@Component({
  selector: 'app-new-account-step-contributions',
  imports: [FormsModule, Button, InputNumber, Message, PlanSettingsForm],
  templateUrl: './new-account-step-contributions.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewAccountStepContributions {
  protected readonly draft = inject(NewAccountDraft);
}

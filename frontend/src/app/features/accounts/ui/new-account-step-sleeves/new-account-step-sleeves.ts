import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Slider } from 'primeng/slider';
import { StrategyPicker } from '../../../strategies/ui/strategy-picker/strategy-picker';
import { NewAccountDraft } from '../../data-access/new-account-draft';

@Component({
  selector: 'app-new-account-step-sleeves',
  imports: [TranslocoPipe, FormsModule, Button, InputText, Message, Slider, StrategyPicker],
  templateUrl: './new-account-step-sleeves.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewAccountStepSleeves {
  protected readonly draft = inject(NewAccountDraft);
}

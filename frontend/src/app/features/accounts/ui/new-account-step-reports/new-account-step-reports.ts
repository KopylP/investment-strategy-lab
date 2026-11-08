import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { SelectButton } from 'primeng/selectbutton';
import { ReportTimingEditor } from '../../../reports/ui/report-timing-editor/report-timing-editor';
import { NewAccountDraft } from '../../data-access/new-account-draft';

@Component({
  selector: 'app-new-account-step-reports',
  imports: [TranslocoPipe, FormsModule, Button, Message, SelectButton, ReportTimingEditor],
  templateUrl: './new-account-step-reports.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewAccountStepReports {
  protected readonly draft = inject(NewAccountDraft);
}

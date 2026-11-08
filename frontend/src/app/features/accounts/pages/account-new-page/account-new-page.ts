import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Step, StepList, StepPanel, StepPanels, Stepper } from 'primeng/stepper';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { NewAccountDraft } from '../../data-access/new-account-draft';
import { NewAccountStepAccount } from '../../ui/new-account-step-account/new-account-step-account';
import { NewAccountStepSleeves } from '../../ui/new-account-step-sleeves/new-account-step-sleeves';
import { NewAccountStepContributions } from '../../ui/new-account-step-contributions/new-account-step-contributions';
import { NewAccountStepReports } from '../../ui/new-account-step-reports/new-account-step-reports';
import { NewAccountStepReview } from '../../ui/new-account-step-review/new-account-step-review';
import { NewAccountStepDone } from '../../ui/new-account-step-done/new-account-step-done';

@Component({
  selector: 'app-account-new-page',
  imports: [TranslocoPipe, 
    Stepper,
    StepList,
    Step,
    StepPanels,
    StepPanel,
    PageHeader,
    NewAccountStepAccount,
    NewAccountStepSleeves,
    NewAccountStepContributions,
    NewAccountStepReports,
    NewAccountStepReview,
    NewAccountStepDone,
  ],
  providers: [NewAccountDraft],
  templateUrl: './account-new-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountNewPage {
  protected readonly draft = inject(NewAccountDraft);
}

import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ProgressBar } from 'primeng/progressbar';
import { MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { ReportProgressVm, ReportVm } from '../../models/report-vm';

import { Money } from '../../../../shared/ui/money/money';
import { Viewport } from '../../../../shared/ui/viewport';
@Component({
  selector: 'app-report-progress',
  imports: [Money, ProgressBar, MoneyPipe],
  templateUrl: './report-progress.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportProgress {
  protected readonly viewport = inject(Viewport);

  readonly report = input.required<ReportVm>();
  readonly progress = input.required<ReportProgressVm>();
}

import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { SelectButton } from 'primeng/selectbutton';
import { Textarea } from 'primeng/textarea';
import { ReportActionVm } from '../../models/report-vm';

@Component({
  selector: 'app-skip-dialog',
  imports: [TranslocoPipe, FormsModule, Button, SelectButton, Textarea],
  templateUrl: './skip-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkipDialog {
  private readonly ref = inject(DynamicDialogRef);
  protected readonly action = (inject(DynamicDialogConfig).data as { action: ReportActionVm })
    .action;

  protected readonly presets = [
    translate('reasons.tooSmall'),
    translate('reasons.noCash'),
    translate('reasons.wait'),
    translate('reasons.broker'),
  ].map((label) => ({ label, value: label }));
  protected readonly reason = signal('');

  protected choose(preset: string | null): void {
    if (preset) this.reason.set(preset);
  }

  protected submit(): void {
    if (this.reason().trim()) this.ref.close(this.reason().trim());
  }

  protected cancel(): void {
    this.ref.close();
  }
}

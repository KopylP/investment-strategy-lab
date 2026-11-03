import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { ReportTimingEditor } from '../../../reports/ui/report-timing-editor/report-timing-editor';
import { DraftReportDto } from '../../models/draft-dto';
import { builtInSignalKinds } from '../../models/draft-vm';
import { ReportKindDto } from '../../models/vocabulary-dto';

@Component({
  selector: 'app-builder-reports-dialog',
  imports: [NgTemplateOutlet, FormsModule, Button, Checkbox, Dialog, ReportTimingEditor],
  templateUrl: './reports-dialog.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsDialog {
  readonly visible = model(false);
  readonly inline = input(false);
  readonly reports = input.required<DraftReportDto[]>();
  readonly declarations = input.required<ReportKindDto[]>();
  readonly saved = output<DraftReportDto[]>();

  protected readonly draft = signal<DraftReportDto[]>([]);
  protected readonly signalKinds = builtInSignalKinds;

  constructor() {
    effect(() => {
      if (this.inline()) this.draft.set(cloneReports(this.reports()));
      else if (this.visible()) untracked(() => this.draft.set(cloneReports(this.reports())));
    });
  }

  protected declaration(code: string): ReportKindDto | undefined {
    return this.declarations().find((report) => report.code === code);
  }

  protected update(report: DraftReportDto): void {
    this.draft.update((reports) =>
      reports.map((current) => (current.code === report.code ? report : current)),
    );
    if (this.inline()) this.saved.emit(cloneReports(this.draft()));
  }

  protected save(): void {
    this.saved.emit(cloneReports(this.draft()));
    this.visible.set(false);
  }
}

function cloneReports(reports: DraftReportDto[]): DraftReportDto[] {
  return reports.map((report) => ({
    ...report,
    schedule: { ...report.schedule },
    signalKinds: [...report.signalKinds],
    signalIds: [...report.signalIds],
  }));
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';
import { ReportStatus } from '../../models/report-dto';

const appearance = {
  New: { label: 'Новий', severity: 'info' },
  InProgress: { label: 'У роботі', severity: 'warn' },
  Closed: { label: 'Закритий', severity: 'success' },
  Superseded: { label: 'Замінений', severity: 'secondary' },
} as const;

@Component({
  selector: 'app-report-status-tag',
  imports: [Tag],
  template: `<p-tag [value]="label()" [severity]="severity()" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportStatusTag {
  readonly status = input.required<ReportStatus>();

  protected readonly label = computed(() => appearance[this.status()].label);
  protected readonly severity = computed(() => appearance[this.status()].severity);
}

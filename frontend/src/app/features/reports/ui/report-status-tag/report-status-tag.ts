import { translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';
import { ReportStatus } from '../../models/report-dto';

const appearance = {
  New: { label: translate('reports.status.New'), severity: 'info' },
  InProgress: { label: translate('reports.status.InProgress'), severity: 'warn' },
  Closed: { label: translate('reports.status.Closed'), severity: 'success' },
  Superseded: { label: translate('reports.status.Superseded'), severity: 'secondary' },
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

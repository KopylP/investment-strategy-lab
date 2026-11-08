import { translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';
import { ActionStatus } from '../../models/report-dto';

type Severity = 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast';

const appearance = {
  Pending: {
    label: translate('reports.actionStatus.Pending'),
    severity: 'secondary',
    icon: 'pi pi-clock',
  },
  Executed: {
    label: translate('reports.actionStatus.Executed'),
    severity: 'success',
    icon: 'pi pi-check',
  },
  PartiallyExecuted: {
    label: translate('reports.actionStatus.PartiallyExecuted'),
    severity: 'warn',
    icon: 'pi pi-percentage',
  },
  Skipped: {
    label: translate('reports.actionStatus.Skipped'),
    severity: 'contrast',
    icon: 'pi pi-forward',
  },
} as const satisfies Record<ActionStatus, { label: string; severity: Severity; icon: string }>;

@Component({
  selector: 'app-action-status-tag',
  imports: [Tag],
  template: `<p-tag [value]="label()" [severity]="severity()" [icon]="icon()" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActionStatusTag {
  readonly status = input.required<ActionStatus>();
  readonly fillSummary = input<string>();

  protected readonly severity = computed(() => appearance[this.status()].severity);
  protected readonly icon = computed(() => appearance[this.status()].icon);
  protected readonly label = computed(() => this.fillSummary() ?? appearance[this.status()].label);
}

import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Button } from 'primeng/button';
import { SplitButton } from 'primeng/splitbutton';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { Message } from 'primeng/message';
import { DatePipe, QuantityPipe } from '../../../../shared/pipes/format-pipes';
import { ReportActionVm } from '../../models/report-vm';
import { ActionStatusTag } from '../action-status-tag/action-status-tag';

import { Money } from '../../../../shared/ui/money/money';
import { Viewport } from '../../../../shared/ui/viewport';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
@Component({
  selector: 'app-report-actions-table',
  imports: [Money, NgTemplateOutlet, TableModule, Button, SplitButton, Tooltip, TapTooltip, Message, ActionStatusTag, QuantityPipe, DatePipe],
  templateUrl: './report-actions-table.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportActionsTable {
  protected readonly viewport = inject(Viewport);

  readonly actions = input.required<ReportActionVm[]>();
  readonly busyActionId = input<string>();

  readonly markExecuted = output<ReportActionVm>();
  readonly partialFill = output<ReportActionVm>();
  readonly skip = output<ReportActionVm>();
  readonly undo = output<ReportActionVm>();

  /** Built once per actions change: a new array on every check would re-render the open menu under the cursor. */
  protected readonly secondaryActions = computed(
    () =>
      new Map<string, MenuItem[]>(
        this.actions().map((action) => [
          action.id,
          [
            { label: 'Частково виконано', icon: 'pi pi-percentage', disabled: !action.canPartiallyFill, command: () => this.partialFill.emit(action) },
            { label: 'Пропустити', icon: 'pi pi-forward', disabled: !action.canSkip, command: () => this.skip.emit(action) },
          ],
        ]),
      ),
  );

  /** Phone cards whose breakdown (split by sub-portfolio, fills) is open. */
  protected readonly expanded = signal(new Set<string>());

  protected toggle(actionId: string): void {
    this.expanded.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(actionId)) next.add(actionId);
      return next;
    });
  }
}

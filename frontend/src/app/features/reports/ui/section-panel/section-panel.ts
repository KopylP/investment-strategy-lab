import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import { MoneyPipe, PercentPipe, QuantityPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { SectionColumnDto } from '../../models/report-dto';
import { SectionVm } from '../../models/report-vm';

/** Why the plan of one sub-portfolio looks the way it does: market, picks, orders before netting, before → after. */
@Component({
  selector: 'app-section-panel',
  imports: [TableModule, Tag, Message, Tooltip, DecimalPipe, MoneyPipe, PercentPipe, SignedPercentPipe, QuantityPipe, SignedValue],
  templateUrl: './section-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SectionPanel {
  readonly section = input.required<SectionVm>();
  readonly currency = input.required<string>();

  /** Numbers right-aligned; a column the strategy did not mark compact only from the sm breakpoint up. */
  protected cellClass(column: SectionColumnDto): string {
    const numeric = column.format !== 'text' && column.format !== 'trend';
    return [numeric ? 'text-right' : '', column.compact ? '' : 'hidden sm:table-cell'].join(' ').trim();
  }

  /** A cell's value as a number for the formatting pipes; anything else (missing, text) reads as no value. */
  protected number(value: number | boolean | string | null): number | null {
    return typeof value === 'number' ? value : null;
  }
}

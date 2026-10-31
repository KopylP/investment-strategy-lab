import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import {
  MoneyPipe,
  PercentPipe,
  QuantityPipe,
  SignedPercentPipe,
} from '../../../../shared/pipes/format-pipes';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { SectionColumnDto } from '../../models/report-dto';
import { SectionVm } from '../../models/report-vm';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';

@Component({
  selector: 'app-section-panel',
  imports: [
    TableModule,
    Tag,
    Message,
    Tooltip,
    TapTooltip,
    DecimalPipe,
    MoneyPipe,
    PercentPipe,
    SignedPercentPipe,
    QuantityPipe,
    SignedValue,
  ],
  templateUrl: './section-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SectionPanel {
  readonly section = input.required<SectionVm>();
  readonly currency = input.required<string>();

  protected cellClass(column: SectionColumnDto): string {
    const numeric = column.format !== 'text' && column.format !== 'trend';
    return [numeric ? 'text-right' : '', column.compact ? '' : 'hidden sm:table-cell']
      .join(' ')
      .trim();
  }

  protected number(value: number | boolean | string | null): number | null {
    return typeof value === 'number' ? value : null;
  }
}

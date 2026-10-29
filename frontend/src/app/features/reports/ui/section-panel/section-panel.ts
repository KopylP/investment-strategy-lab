import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import { MoneyPipe, PercentPipe, QuantityPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { SectionVm } from '../../models/report-vm';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';

/** Why the plan of one sub-portfolio looks the way it does: market, picks, orders before netting, before → after. */
@Component({
  selector: 'app-section-panel',
  imports: [TableModule, Tag, Message, Tooltip, TapTooltip, MoneyPipe, PercentPipe, SignedPercentPipe, QuantityPipe, SignedValue],
  templateUrl: './section-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SectionPanel {
  readonly section = input.required<SectionVm>();
  readonly currency = input.required<string>();
}

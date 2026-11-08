import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { DateTimePipe } from '../../../../shared/pipes/format-pipes';
import { StatementImportStore } from '../../data-access/statement-import-store';
import { Viewport } from '../../../../shared/ui/viewport';

@Component({
  selector: 'app-statement-history-card',
  imports: [TranslocoPipe, Button, TableModule, Tag, DateTimePipe],
  templateUrl: './statement-history-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatementHistoryCard {
  readonly undo = output<string>();
  protected readonly store = inject(StatementImportStore);
  protected readonly viewport = inject(Viewport);
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Button } from 'primeng/button';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { DatePipe, MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { InstrumentsStore } from '../../data-access/instruments-store';

@Component({
  selector: 'app-instrument-list-page',
  imports: [Button, Skeleton, TableModule, Tag, PageHeader, MoneyPipe, DatePipe],
  providers: [InstrumentsStore],
  templateUrl: './instrument-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstrumentListPage {
  protected readonly store = inject(InstrumentsStore);
}

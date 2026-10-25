import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { DateTimePipe, MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { ReportListStore } from '../../data-access/report-list-store';
import { ReportStatusTag } from '../../ui/report-status-tag/report-status-tag';

@Component({
  selector: 'app-report-list-page',
  imports: [FormsModule, RouterLink, Button, Dialog, InputNumber, Message, Skeleton, TableModule, Tag, PageHeader, EmptyState, ReportStatusTag, MoneyPipe, DateTimePipe],
  providers: [ReportListStore],
  templateUrl: './report-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportListPage {
  protected readonly store = inject(ReportListStore);

  protected readonly generateVisible = signal(false);
  protected readonly contribution = signal<number | null>(null);

  protected openGenerate(): void {
    this.contribution.set(this.store.monthlyContribution());
    this.generateVisible.set(true);
  }

  protected async generate(): Promise<void> {
    const settingsAmount = this.store.monthlyContribution();
    const amount = this.contribution();
    // The settings amount is sent as "default" so a contribution already paid this month is not planned twice (D14).
    await this.store.generateMonthly(amount === settingsAmount ? null : amount);
    this.generateVisible.set(false);
  }
}

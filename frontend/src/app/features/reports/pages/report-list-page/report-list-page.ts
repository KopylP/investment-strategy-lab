import { NgTemplateOutlet } from '@angular/common';
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
import { DatePipe, DateTimePipe, MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { ReportListStore } from '../../data-access/report-list-store';
import { reportIcon, ScheduledReport } from '../../models/report-schedule';
import { NextReports } from '../../ui/next-reports/next-reports';
import { ReportStatusTag } from '../../ui/report-status-tag/report-status-tag';

@Component({
  selector: 'app-report-list-page',
  imports: [FormsModule, NgTemplateOutlet, RouterLink, Button, Dialog, InputNumber, Message, Skeleton, TableModule, Tag, PageHeader, EmptyState, NextReports, ReportStatusTag, MoneyPipe, DatePipe, DateTimePipe],
  providers: [ReportListStore],
  templateUrl: './report-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportListPage {
  protected readonly store = inject(ReportListStore);

  protected readonly icon = reportIcon;
  /** The rebalance report whose contribution is being asked for. */
  protected readonly asking = signal<ScheduledReport | null>(null);
  protected readonly contribution = signal<number | null>(null);

  /** A rebalance report asks for the month's contribution first; any other is made at once. */
  protected start(report: ScheduledReport): void {
    if (report.mode !== 'rebalance') {
      void this.store.generate(report);
      return;
    }
    this.contribution.set(this.store.monthlyContribution());
    this.asking.set(report);
  }

  protected async generate(): Promise<void> {
    const report = this.asking();
    if (!report) return;
    const settingsAmount = this.store.monthlyContribution();
    const amount = this.contribution();
    // The settings amount is sent as "default" so a contribution already paid this month is not planned twice (D14).
    await this.store.generate(report, amount === settingsAmount ? null : amount);
    this.asking.set(null);
  }
}

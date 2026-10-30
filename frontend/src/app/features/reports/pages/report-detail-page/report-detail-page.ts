import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ConfirmationService, MenuItem } from 'primeng/api';
import { Button } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { Menu } from 'primeng/menu';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';
import { Tag } from 'primeng/tag';
import { filter } from 'rxjs';
import { DatePipe, DateTimePipe, MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { Viewport } from '../../../../shared/ui/viewport';
import { ReportDetailStore } from '../../data-access/report-detail-store';
import { FillInput, ReportActionVm } from '../../models/report-vm';
import { FillDialog } from '../../ui/fill-dialog/fill-dialog';
import { LoadOperationsData, LoadOperationsDialog } from '../../ui/load-operations-dialog/load-operations-dialog';
import { NextReports } from '../../ui/next-reports/next-reports';
import { ReportActionsTable } from '../../ui/report-actions-table/report-actions-table';
import { ReportProgress } from '../../ui/report-progress/report-progress';
import { ReportStatusTag } from '../../ui/report-status-tag/report-status-tag';
import { SectionPanel } from '../../ui/section-panel/section-panel';
import { SkipDialog } from '../../ui/skip-dialog/skip-dialog';

@Component({
  selector: 'app-report-detail-page',
  imports: [
    RouterLink, Button, Menu, Message, Skeleton, Tag, Tabs, TabList, Tab, TabPanels, TabPanel,
    PageHeader, ReportProgress, ReportActionsTable, NextReports, ReportStatusTag, SectionPanel, DatePipe, DateTimePipe, MoneyPipe,
  ],
  providers: [ReportDetailStore],
  templateUrl: './report-detail-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportDetailPage {
  protected readonly store = inject(ReportDetailStore);
  private readonly dialogs = inject(DialogService);
  private readonly confirm = inject(ConfirmationService);
  private readonly router = inject(Router);
  protected readonly viewport = inject(Viewport);

  readonly reportId = input.required<string>();

  /** The header buttons as one menu on a phone. */
  protected readonly pageActions = computed<MenuItem[]>(() => {
    const report = this.store.report();
    const busy = this.store.busy();
    return [
      ...(this.store.canMark() ? [{ label: 'Довантажити операції', icon: 'pi pi-download', command: () => this.openLoadOperations() }] : []),
      ...(report && report.summary.status !== 'Superseded'
        ? [{ label: 'Згенерувати заново', icon: 'pi pi-refresh', disabled: busy === 'regenerate', command: () => this.confirmRegenerate() }]
        : []),
      ...(this.store.canMark() ? [{ label: 'Закрити рапорт', icon: 'pi pi-lock', disabled: busy === 'close', command: () => this.confirmClose() }] : []),
    ];
  });

  constructor() {
    effect(() => this.store.open(this.reportId()));
  }

  protected openFill(action: ReportActionVm): void {
    this.openFillDialog(action, 'full', (fill) => this.store.markExecuted(action, fill));
  }

  protected openPartialFill(action: ReportActionVm): void {
    this.openFillDialog(action, 'partial', (fill) => this.store.recordPartialFill(action, fill));
  }

  protected openSkip(action: ReportActionVm): void {
    this.dialogs
      .open(SkipDialog, { header: `Пропустити · ${action.title}`, modal: true, width: '32rem', closable: true, data: { action }, breakpoints: { '640px': '95vw' } })
      ?.onClose.pipe(filter((reason): reason is string => !!reason))
      .subscribe((reason) => void this.store.skip(action, reason));
  }

  protected openLoadOperations(): void {
    const report = this.store.report();
    if (!report) return;
    const data: LoadOperationsData = { reportId: report.summary.id, accountId: report.summary.accountId, currency: report.summary.currency, actions: this.store.actions() };
    this.dialogs
      .open(LoadOperationsDialog, { header: 'Довантажити операції', modal: true, width: '40rem', closable: true, data, breakpoints: { '640px': '95vw' } })
      ?.onClose.pipe(filter((count): count is number => typeof count === 'number'))
      .subscribe((count) => this.store.marked(count));
  }

  protected confirmUndo(action: ReportActionVm): void {
    const linkedOnly = action.fills.length > 0 && action.fills.every((fill) => fill.linked);
    this.confirm.confirm({
      header: 'Скасувати відмітку?',
      message: linkedOnly
        ? 'Дія знову стане очікуваною. Операції з виписки лишаться в журналі — їх лише буде відв’язано від плану.'
        : action.fills.length
          ? 'Дія знову стане очікуваною, а операції в журналі буде сторновано.'
          : 'Дія знову стане очікуваною.',
      icon: 'pi pi-undo',
      acceptLabel: 'Скасувати відмітку',
      rejectLabel: 'Залишити',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.undo(action),
    });
  }

  protected confirmClose(): void {
    this.confirm.confirm({
      header: 'Закрити рапорт?',
      message: 'Дії без відмітки буде позначено як пропущені («закрито без відмітки»). Відмітки можна буде скасувати й пізніше.',
      icon: 'pi pi-lock',
      acceptLabel: 'Закрити',
      rejectLabel: 'Скасувати',
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.close(),
    });
  }

  protected confirmRegenerate(): void {
    this.confirm.confirm({
      header: 'Згенерувати заново?',
      message: 'Новий план рахується від фактичних позицій і кешу; цей рапорт буде замінено. Уже зроблені відмітки лишаються в журналі.',
      icon: 'pi pi-refresh',
      acceptLabel: 'Згенерувати',
      rejectLabel: 'Скасувати',
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: async () => {
        const requestId = await this.store.regenerate();
        if (requestId) await this.router.navigate(['/reports'], { queryParams: { request: requestId } });
      },
    });
  }

  private openFillDialog(action: ReportActionVm, mode: 'full' | 'partial', save: (fill: FillInput) => Promise<void>): void {
    this.dialogs
      .open(FillDialog, {
        header: `${mode === 'partial' ? 'Частково виконано' : 'Виконано'} · ${action.title}`,
        modal: true,
        width: '36rem',
        closable: true,
        data: { action, mode },
        breakpoints: { '640px': '95vw' },
      })
      ?.onClose.pipe(filter((fill): fill is FillInput => !!fill))
      .subscribe((fill) => void save(fill));
  }
}

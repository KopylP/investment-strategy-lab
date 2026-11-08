import { TranslocoPipe, translate } from '@jsverse/transloco';
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
import { EnteredFill, ReportActionVm } from '../../models/report-vm';
import { FillDialog } from '../../ui/fill-dialog/fill-dialog';
import {
  LoadOperationsData,
  LoadOperationsDialog,
} from '../../ui/load-operations-dialog/load-operations-dialog';
import { ReportActionsTable } from '../../ui/report-actions-table/report-actions-table';
import { ReportProgress } from '../../ui/report-progress/report-progress';
import { ReportStatusTag } from '../../ui/report-status-tag/report-status-tag';
import { SectionPanel } from '../../ui/section-panel/section-panel';
import { SkipDialog } from '../../ui/skip-dialog/skip-dialog';

@Component({
  selector: 'app-report-detail-page',
  imports: [
    TranslocoPipe,
    RouterLink,
    Button,
    Menu,
    Message,
    Skeleton,
    Tag,
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    PageHeader,
    ReportProgress,
    ReportActionsTable,
    ReportStatusTag,
    SectionPanel,
    DatePipe,
    DateTimePipe,
    MoneyPipe,
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

  protected readonly pageActions = computed<MenuItem[]>(() => {
    const report = this.store.report();
    const busy = this.store.busy();

    return [
      ...(this.store.canMark()
        ? [
            {
              label: translate('reports.detail.loadOperations'),
              icon: 'pi pi-download',
              command: () => this.openLoadOperations(),
            },
          ]
        : []),
      ...(report && report.summary.status !== 'Superseded'
        ? [
            {
              label: translate('reports.detail.regenerate'),
              icon: 'pi pi-refresh',
              disabled: busy === 'regenerate',
              command: () => this.confirmRegenerate(),
            },
          ]
        : []),
      ...(this.store.canMark()
        ? [
            {
              label: translate('reports.detail.close'),
              icon: 'pi pi-lock',
              disabled: busy === 'close',
              command: () => this.confirmClose(),
            },
          ]
        : []),
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
      .open(SkipDialog, {
        header: translate('reports.detail.skipTitle', { title: action.title }),
        modal: true,
        width: '32rem',
        closable: true,
        data: { action },
        breakpoints: { '640px': '95vw' },
      })
      ?.onClose.pipe(filter((reason): reason is string => !!reason))
      .subscribe((reason) => void this.store.skip(action, reason));
  }

  protected openLoadOperations(): void {
    const report = this.store.report();
    if (!report) return;

    const data: LoadOperationsData = {
      reportId: report.summary.id,
      accountId: report.summary.accountId,
      currency: report.summary.currency,
      actions: this.store.actions(),
    };

    this.dialogs
      .open(LoadOperationsDialog, {
        header: translate('reports.detail.loadOperations'),
        modal: true,
        width: '40rem',
        closable: true,
        data,
        breakpoints: { '640px': '95vw' },
      })
      ?.onClose.pipe(filter((count): count is number => typeof count === 'number'))
      .subscribe((count) => this.store.marked(count));
  }

  protected confirmUndo(action: ReportActionVm): void {
    const linkedOnly = action.fills.length > 0 && action.fills.every((fill) => fill.linked);

    this.confirm.confirm({
      header: translate('reports.detail.undoQuestion'),
      message: linkedOnly
        ? translate('reports.detail.undoStatementHint')
        : action.fills.length
          ? translate('reports.detail.undoReverseHint')
          : translate('reports.detail.undoHint'),
      icon: 'pi pi-undo',
      acceptLabel: translate('reports.actions.undoMark'),
      rejectLabel: translate('reports.detail.keep'),
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.undo(action),
    });
  }

  protected confirmClose(): void {
    this.confirm.confirm({
      header: translate('reports.detail.closeQuestion'),
      message: translate('reports.detail.closeHint'),
      icon: 'pi pi-lock',
      acceptLabel: translate('common.close'),
      rejectLabel: translate('common.cancel'),
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.close(),
    });
  }

  protected confirmRegenerate(): void {
    this.confirm.confirm({
      header: translate('reports.detail.regenerateQuestion'),
      message: translate('reports.detail.regenerateHint'),
      icon: 'pi pi-refresh',
      acceptLabel: translate('reports.list.generate'),
      rejectLabel: translate('common.cancel'),
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: async () => {
        const requestId = await this.store.regenerate();
        if (requestId)
          await this.router.navigate(['/reports'], { queryParams: { request: requestId } });
      },
    });
  }

  private openFillDialog(
    action: ReportActionVm,
    mode: 'full' | 'partial',
    save: (fill: EnteredFill) => Promise<void>,
  ): void {
    this.dialogs
      .open(FillDialog, {
        header: `${mode === 'partial' ? translate('reports.actions.partlyDone') : translate('reports.actionStatus.Executed')} · ${action.title}`,
        modal: true,
        width: '36rem',
        closable: true,
        data: { action, mode },
        breakpoints: { '640px': '95vw' },
      })
      ?.onClose.pipe(filter((fill): fill is EnteredFill => !!fill))
      .subscribe((fill) => void save(fill));
  }
}

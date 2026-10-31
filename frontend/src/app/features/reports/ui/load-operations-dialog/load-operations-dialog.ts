import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { ApiClient, toProblemMessage } from '../../../../core/api/api-client';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { DatePipe, MoneyPipe, QuantityPipe } from '../../../../shared/pipes/format-pipes';
import { FileDrop } from '../../../../shared/ui/file-drop';
import { LoadOperationsPreviewDto, MatchedOperationDto } from '../../models/report-dto';
import { ReportActionVm } from '../../models/report-vm';
import { ReportsApi } from '../../data-access/reports-api';

export interface LoadOperationsData {
  reportId: string;
  accountId: string;
  currency: string;
  actions: ReportActionVm[];
}

interface StatementUpload {
  id: string;
  status: string;
  counts: { toRecord: number; needsMapping: number; alreadyImported: number };
}

@Component({
  selector: 'app-load-operations-dialog',
  imports: [RouterLink, Button, Message, Skeleton, Tag, FileDrop, DatePipe, MoneyPipe, QuantityPipe],
  templateUrl: './load-operations-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadOperationsDialog implements OnInit {
  private readonly ref = inject(DynamicDialogRef);
  private readonly api = inject(ApiClient);
  private readonly reports = inject(ReportsApi);
  private readonly catalog = inject(InstrumentCatalog);
  protected readonly data = inject(DynamicDialogConfig).data as LoadOperationsData;

  protected readonly preview = signal<LoadOperationsPreviewDto | null>(null);
  protected readonly upload = signal<StatementUpload | null>(null);
  protected readonly busy = signal<'preview' | 'upload' | 'import' | 'apply' | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly matches = computed(() => {
    const byId = new Map(this.data.actions.map((action) => [action.id, action]));
    return (this.preview()?.matches ?? []).map((match) => ({ ...match, action: byId.get(match.actionId) }));
  });

  ngOnInit(): void {
    void this.loadPreview();
  }

  protected symbol(instrumentId: string | null): string {
    return this.catalog.symbol(instrumentId);
  }

  protected operationLabel(operation: MatchedOperationDto): string {
    return { Buy: 'Купівля', Sell: 'Продаж', Contribution: 'Поповнення' }[operation.kind] ?? operation.kind;
  }

  protected async onFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    await this.run('upload', async () => {
      this.upload.set(await this.api.post<StatementUpload>(`/api/portfolio/accounts/${this.data.accountId}/statement-imports`, { fileName: file.name, content: await file.text() }));
    });
  }

  protected async importStatement(): Promise<void> {
    const upload = this.upload();
    if (!upload) return;
    await this.run('import', async () => {
      this.upload.set(await this.api.post<StatementUpload>(`/api/portfolio/accounts/${this.data.accountId}/statement-imports/${upload.id}/confirm`));
      await this.loadPreview();
    });
  }

  protected async apply(): Promise<void> {
    await this.run('apply', async () => {
      const marked = await this.reports.applyJournalMatches(this.data.reportId);
      this.ref.close(marked);
    });
  }

  protected close(): void {
    this.ref.close();
  }

  private async loadPreview(): Promise<void> {
    await this.run('preview', async () => this.preview.set(await this.reports.previewJournalMatches(this.data.reportId)));
  }

  private async run(kind: 'preview' | 'upload' | 'import' | 'apply', work: () => Promise<void>): Promise<void> {
    this.busy.set(kind);
    this.error.set(null);
    try {
      await work();
    } catch (error) {
      this.error.set(toProblemMessage(error));
    } finally {
      this.busy.set(null);
    }
  }
}

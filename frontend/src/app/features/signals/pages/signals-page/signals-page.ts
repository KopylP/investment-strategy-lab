import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignalsStore } from '../../data-access/signals-store';
import { SignalDto, SignalOccurrenceDto, SignalTargetKind } from '../../models/signal-dto';
import { clearReasonLabel, conditionLabel, signalScheduleLabel, valueLabel } from '../../models/signal-labels';
import { SignalEditor } from '../../ui/signal-editor/signal-editor';
import { SignalRow, SignalRowTone } from '../../ui/signal-row/signal-row';

const targetIcons: Record<SignalTargetKind, string> = {
  Instrument: 'pi pi-chart-line',
  InstrumentGroup: 'pi pi-objects-column',
  EachPosition: 'pi pi-chart-pie',
  SubPortfolio: 'pi pi-sitemap',
  Account: 'pi pi-wallet',
};

@Component({
  selector: 'app-signals-page',
  imports: [Button, Message, Skeleton, Tag, DatePipe, PageHeader, SignalEditor, SignalRow],
  providers: [SignalsStore],
  templateUrl: './signals-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignalsPage {
  protected readonly store = inject(SignalsStore);

  protected readonly editorVisible = signal(false);
  protected readonly editing = signal<SignalDto | null>(null);

  protected readonly clearReasonLabel = clearReasonLabel;

  protected create(): void {
    this.editing.set(null);
    this.editorVisible.set(true);
  }

  protected edit(item: SignalDto): void {
    this.editing.set(item);
    this.editorVisible.set(true);
  }

  protected openSignalOf(occurrence: SignalOccurrenceDto): void {
    const item = this.store.signals().find((each) => each.id === occurrence.signalId);

    if (item) this.edit(item);
  }

  protected subtitleOf(item: SignalDto): string {
    return `${this.store.targetLabel(item)} · ${conditionLabel(item.condition)}`;
  }

  protected targetIcon(item: SignalDto): string {
    return targetIcons[item.target];
  }

  protected toneOf(item: SignalDto): SignalRowTone {
    if (!item.enabled) return 'muted';

    return item.activeCount ? 'danger' : 'primary';
  }

  protected detailOf(item: SignalDto): string {
    const parts = [signalScheduleLabel(item.schedule)];
    const reports = this.store.rulesOf(item.id).length;

    if (reports) parts.push(`рапортів: ${reports}`);
    if (!this.store.notifies(item.id)) parts.push('без сповіщень');

    return parts.join(' · ');
  }

  protected latestValue(occurrence: SignalOccurrenceDto): string {
    return valueLabel(occurrence.metric, occurrence.latestValue);
  }

  protected triggerValue(occurrence: SignalOccurrenceDto): string {
    return valueLabel(occurrence.metric, occurrence.triggerValue);
  }
}

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignalsStore } from '../../data-access/signals-store';
import { SignalDto, SignalOccurrenceDto } from '../../models/signal-dto';
import {
  clearReasonLabel,
  conditionLabel,
  signalScheduleLabel,
  valueLabel,
} from '../../models/signal-labels';
import { SignalEditor } from '../../ui/signal-editor/signal-editor';

@Component({
  selector: 'app-signals-page',
  imports: [Button, Message, Skeleton, Tag, DatePipe, PageHeader, SignalEditor],
  providers: [SignalsStore],
  templateUrl: './signals-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignalsPage {
  protected readonly store = inject(SignalsStore);

  protected readonly editorVisible = signal(false);
  protected readonly editing = signal<SignalDto | null>(null);

  protected readonly conditionLabel = conditionLabel;
  protected readonly scheduleLabel = signalScheduleLabel;
  protected readonly clearReasonLabel = clearReasonLabel;

  protected create(): void {
    this.editing.set(null);
    this.editorVisible.set(true);
  }

  protected edit(item: SignalDto): void {
    this.editing.set(item);
    this.editorVisible.set(true);
  }

  protected latestValue(occurrence: SignalOccurrenceDto): string {
    return valueLabel(occurrence.metric, occurrence.latestValue);
  }

  protected triggerValue(occurrence: SignalOccurrenceDto): string {
    return valueLabel(occurrence.metric, occurrence.triggerValue);
  }
}

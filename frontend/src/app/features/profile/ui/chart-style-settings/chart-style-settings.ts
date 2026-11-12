import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { MessageService } from 'primeng/api';
import { Skeleton } from 'primeng/skeleton';
import { SelectButton } from 'primeng/selectbutton';
import { toProblemMessage } from '../../../../core/api/api-client';
import { ChartStyle, ChartStyleName, chartStyles } from '../../../../core/context/chart-style';

@Component({
  selector: 'app-chart-style-settings',
  host: { class: 'contents' },
  imports: [FormsModule, TranslocoPipe, SelectButton, Skeleton],
  templateUrl: './chart-style-settings.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChartStyleSettingsCard {
  private readonly chartStyle = inject(ChartStyle);
  private readonly messages = inject(MessageService);

  protected readonly saving = signal(false);
  protected readonly selected = this.chartStyle.choice;
  protected readonly options = computed(() =>
    chartStyles.map((style) => ({
      value: style,
      label: translate(`profile.chartStyle.names.${style}`),
    })),
  );

  protected async choose(style: ChartStyleName | null): Promise<void> {
    if (!style) return;

    this.saving.set(true);

    try {
      await this.chartStyle.choose(style);
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: translate('profile.notSaved'),
        detail: toProblemMessage(error),
      });
    } finally {
      this.saving.set(false);
    }
  }
}

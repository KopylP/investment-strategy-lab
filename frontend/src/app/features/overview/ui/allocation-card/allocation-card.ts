import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UIChart } from 'primeng/chart';
import { TableModule } from 'primeng/table';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { PercentPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { Money } from '../../../../shared/ui/money/money';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { Viewport } from '../../../../shared/ui/viewport';
import { DashboardDto } from '../../data-access/dashboard-api';

@Component({
  selector: 'app-allocation-card',
  imports: [RouterLink, UIChart, TableModule, Money, PercentPipe, SignedPercentPipe, SignedValue],
  templateUrl: './allocation-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AllocationCard {
  private readonly theme = inject(ChartTheme);
  protected readonly viewport = inject(Viewport);

  readonly dashboard = input.required<DashboardDto>();

  protected readonly data = computed(() => {
    const palette = this.theme.palette();
    const subs = this.dashboard().subPortfolios;

    return {
      labels: subs.map((sub) => sub.name),
      datasets: [
        {
          label: 'Факт',
          data: subs.map((sub) => sub.actualShare * 100),
          backgroundColor: palette.series,
        },
        {
          label: 'Ціль',
          data: subs.map((sub) => sub.targetShare * 100),
          backgroundColor: palette.series.map((color) => withAlpha(color, 0.35)),
        },
      ],
    };
  });

  protected readonly options = computed(() => ({
    cutout: '55%',
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (context: { dataset: { label: string }; label: string; parsed: number }) =>
            `${context.label} · ${context.dataset.label}: ${context.parsed.toFixed(1)}%`,
        },
      },
    },
  }));

  protected readonly colors = computed(() => this.theme.palette().series);
}

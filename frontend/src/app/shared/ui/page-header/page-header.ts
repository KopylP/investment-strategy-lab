import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { NavigationHistory } from '../../../core/layout/navigation-history';

@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly backLink = input<string>();
  readonly backLabel = input<string>('Назад');

  private readonly history = inject(NavigationHistory);
  protected readonly backTitle = computed(() => this.history.previous()?.title ?? this.backLabel());

  protected back(parentUrl: string): void {
    this.history.back(parentUrl);
  }
}

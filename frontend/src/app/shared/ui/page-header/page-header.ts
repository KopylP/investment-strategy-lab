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
  /** The parent page of a page below a list: where «back» leads when the user came here from outside the app. */
  readonly backLink = input<string>();
  readonly backLabel = input<string>('Назад');

  private readonly history = inject(NavigationHistory);
  /** «back» names the page it returns to: where the user came from, else the parent. */
  protected readonly backTitle = computed(() => this.history.previous()?.title ?? this.backLabel());

  protected back(parentUrl: string): void {
    this.history.back(parentUrl);
  }
}

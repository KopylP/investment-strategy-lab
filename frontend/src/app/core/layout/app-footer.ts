import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  // On the page's grey, not a card: a shade darker than the muted colour in the light theme keeps 4.5:1.
  template: `<div class="layout-footer text-surface-600 dark:text-muted-color text-sm">Investment Lab · розрахунки — оцінка, рішення ухвалюєте ви</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppFooter {}

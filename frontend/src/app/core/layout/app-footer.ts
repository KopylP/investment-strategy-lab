import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  imports: [TranslocoPipe],
  template: `<div class="layout-footer text-surface-600 dark:text-muted-color text-sm">
    {{ 'layout.footer' | transloco }}
  </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppFooter {}

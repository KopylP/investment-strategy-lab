import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  template: `<div class="layout-footer text-muted-color text-sm">Investment Lab · розрахунки — оцінка, рішення ухвалюєте ви</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppFooter {}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app-menuitem';
import { OpenActionsBadge } from '../notifications/open-actions-badge';

@Component({
  selector: 'app-menu',
  imports: [AppMenuitem],
  template: `<ul class="layout-menu">
    @for (item of model(); track item.label) {
      @if (!item.separator) {
        <li app-menuitem [item]="item" [root]="true"></li>
      } @else {
        <li class="menu-separator"></li>
      }
    }
  </ul>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppMenu {
  private readonly openActions = inject(OpenActionsBadge);

  protected readonly model = computed<MenuItem[]>(() => [
    {
      label: 'Інвестування',
      items: [
        { label: 'Огляд', icon: 'pi pi-fw pi-home', routerLink: ['/'] },
        {
          label: this.openActions.count() ? `Рапорти · ${this.openActions.count()}` : 'Рапорти',
          icon: 'pi pi-fw pi-list-check',
          routerLink: ['/reports'],
        },
        { label: 'Сигнали', icon: 'pi pi-fw pi-bell', routerLink: ['/signals'] },
        { label: 'Позиції', icon: 'pi pi-fw pi-chart-pie', routerLink: ['/holdings'] },
        { label: 'Журнал операцій', icon: 'pi pi-fw pi-book', routerLink: ['/transactions'] },
        { label: 'Податки', icon: 'pi pi-fw pi-percentage', routerLink: ['/taxes'] },
      ],
    },
    {
      label: 'Налаштування',
      items: [
        { label: 'Рахунки', icon: 'pi pi-fw pi-wallet', routerLink: ['/accounts'] },
        { label: 'Стратегії', icon: 'pi pi-fw pi-sliders-h', routerLink: ['/strategies'] },
        { label: 'Інструменти й котирування', icon: 'pi pi-fw pi-chart-line', routerLink: ['/instruments'] },
        { label: 'Профіль', icon: 'pi pi-fw pi-user', routerLink: ['/profile'] },
      ],
    },
  ]);
}

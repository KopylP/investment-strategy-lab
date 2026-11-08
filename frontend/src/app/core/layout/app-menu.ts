import { translate } from '@jsverse/transloco';
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
      label: translate('layout.menu.investing'),
      items: [
        { label: translate('layout.menu.overview'), icon: 'pi pi-fw pi-home', routerLink: ['/'] },
        {
          label: this.openActions.count()
            ? translate('layout.menu.reportsWithCount', { count: this.openActions.count() })
            : translate('layout.menu.reports'),
          icon: 'pi pi-fw pi-list-check',
          routerLink: ['/reports'],
        },
        { label: translate('layout.menu.signals'), icon: 'pi pi-fw pi-bell', routerLink: ['/signals'] },
        { label: translate('layout.menu.holdings'), icon: 'pi pi-fw pi-chart-pie', routerLink: ['/holdings'] },
        { label: translate('layout.menu.transactions'), icon: 'pi pi-fw pi-book', routerLink: ['/transactions'] },
        { label: translate('layout.menu.taxes'), icon: 'pi pi-fw pi-percentage', routerLink: ['/taxes'] },
      ],
    },
    {
      label: translate('layout.menu.settings'),
      items: [
        { label: translate('layout.menu.accounts'), icon: 'pi pi-fw pi-wallet', routerLink: ['/accounts'] },
        { label: translate('layout.menu.strategies'), icon: 'pi pi-fw pi-sliders-h', routerLink: ['/strategies'] },
        { label: translate('layout.menu.builder'), icon: 'pi pi-fw pi-sitemap', routerLink: ['/strategy-builder'] },
        {
          label: translate('layout.menu.instruments'),
          icon: 'pi pi-fw pi-chart-line',
          routerLink: ['/instruments'],
        },
        { label: translate('layout.menu.profile'), icon: 'pi pi-fw pi-user', routerLink: ['/profile'] },
      ],
    },
  ]);
}

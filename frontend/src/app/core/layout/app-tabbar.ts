import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { OpenActionsBadge } from '../notifications/open-actions-badge';

interface Tab {
  label: string;
  icon: string;
  link: string;
  exact: boolean;
}

@Component({
  selector: 'app-tabbar',
  imports: [TranslocoPipe, RouterLink, RouterLinkActive],
  template: `<nav class="layout-tabbar" [attr.aria-label]="'layout.menu.sections' | transloco">
    @for (tab of tabs; track tab.link) {
      <a
        class="layout-tabbar-item"
        [routerLink]="tab.link"
        routerLinkActive="active"
        [routerLinkActiveOptions]="{ exact: tab.exact }"
        ariaCurrentWhenActive="page"
      >
        <span class="layout-tabbar-icon">
          <i [class]="tab.icon"></i>
          @if (tab.link === '/reports' && openActions.count(); as count) {
            <span class="layout-tabbar-badge">{{ count }}</span>
          }
        </span>
        <span>{{ tab.label | transloco }}</span>
      </a>
    }
  </nav>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppTabbar {
  protected readonly openActions = inject(OpenActionsBadge);

  protected readonly tabs: Tab[] = [
    { label: 'layout.menu.overview', icon: 'pi pi-home', link: '/', exact: true },
    { label: 'layout.menu.reports', icon: 'pi pi-list-check', link: '/reports', exact: false },
    { label: 'layout.menu.holdings', icon: 'pi pi-chart-pie', link: '/holdings', exact: false },
    { label: 'layout.menu.journal', icon: 'pi pi-book', link: '/transactions', exact: false },
    { label: 'layout.menu.taxes', icon: 'pi pi-percentage', link: '/taxes', exact: false },
  ];
}

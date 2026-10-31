import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppTopbar } from './app-topbar';
import { AppSidebar } from './app-sidebar';
import { AppFooter } from './app-footer';
import { AppTabbar } from './app-tabbar';
import { LayoutService } from './layout-service';

@Component({
  selector: 'app-layout',
  imports: [AppTopbar, AppSidebar, RouterOutlet, AppFooter, AppTabbar],
  template: `<div class="layout-wrapper" [class]="containerClass()">
    <app-topbar />
    <app-sidebar />
    <div class="layout-main-container">
      <div class="layout-main">
        <router-outlet />
      </div>
      <app-footer />
    </div>
    <app-tabbar />
    <div class="layout-mask"></div>
  </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppLayout {
  private readonly layout = inject(LayoutService);

  protected readonly containerClass = computed(() => {
    const config = this.layout.layoutConfig();
    const state = this.layout.layoutState();
    return {
      'layout-overlay': config.menuMode === 'overlay',
      'layout-static': config.menuMode === 'static',
      'layout-static-inactive': state.staticMenuDesktopInactive && config.menuMode === 'static',
      'layout-overlay-active': state.overlayMenuActive,
      'layout-mobile-active': state.mobileMenuActive,
    };
  });

  constructor() {
    effect(() =>
      document.body.classList.toggle('blocked-scroll', this.layout.layoutState().mobileMenuActive),
    );
  }
}

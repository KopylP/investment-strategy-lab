import { Injectable, effect, signal, computed } from '@angular/core';

export interface LayoutConfig {
  preset: string;
  primary: string;
  surface: string | undefined | null;
  darkTheme: boolean;
  menuMode: string;
}

interface LayoutState {
  staticMenuDesktopInactive: boolean;
  overlayMenuActive: boolean;
  configSidebarVisible: boolean;
  mobileMenuActive: boolean;
  menuHoverActive: boolean;
  activePath: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class LayoutService {
  layoutConfig = signal<LayoutConfig>({
    preset: 'Aura',
    primary: 'indigo',
    surface: null,
    darkTheme: readDarkTheme(),
    menuMode: 'static',
  });

  layoutState = signal<LayoutState>({
    staticMenuDesktopInactive: false,
    overlayMenuActive: false,
    configSidebarVisible: false,
    mobileMenuActive: false,
    menuHoverActive: false,
    activePath: null,
  });

  theme = computed(() => (this.layoutConfig().darkTheme ? 'light' : 'dark'));

  isSidebarActive = computed(
    () => this.layoutState().overlayMenuActive || this.layoutState().mobileMenuActive,
  );

  isDarkTheme = computed(() => this.layoutConfig().darkTheme);

  getPrimary = computed(() => this.layoutConfig().primary);

  getSurface = computed(() => this.layoutConfig().surface);

  isOverlay = computed(() => this.layoutConfig().menuMode === 'overlay');

  transitionComplete = signal<boolean>(false);

  private initialized = false;

  constructor() {
    effect(() => {
      const config = this.layoutConfig();

      if (!this.initialized || !config) {
        this.initialized = true;

        return;
      }

      this.handleDarkModeTransition(config);
    });

    systemDarkQuery()?.addEventListener('change', (event) => {
      if (storedDarkTheme() === event.matches) clearDarkTheme();

      if (storedDarkTheme() === null && this.layoutConfig().darkTheme !== event.matches) {
        this.layoutConfig.update((config) => ({ ...config, darkTheme: event.matches }));
      }
    });
  }

  private handleDarkModeTransition(config: LayoutConfig): void {
    const supportsViewTransition = 'startViewTransition' in document;

    if (supportsViewTransition) {
      this.startViewTransition(config);
    } else {
      this.toggleDarkMode(config);
    }
  }

  private startViewTransition(config: LayoutConfig): void {
    document.startViewTransition(() => {
      this.toggleDarkMode(config);
    });
  }

  toggleDarkMode(config?: LayoutConfig): void {
    const _config = config || this.layoutConfig();

    writeDarkTheme(_config.darkTheme);

    if (_config.darkTheme) {
      document.documentElement.classList.add('app-dark');
    } else {
      document.documentElement.classList.remove('app-dark');
    }
  }

  onMenuToggle() {
    if (this.isOverlay()) {
      this.layoutState.update((prev) => ({
        ...prev,
        overlayMenuActive: !this.layoutState().overlayMenuActive,
      }));
    }

    if (this.isDesktop()) {
      this.layoutState.update((prev) => ({
        ...prev,
        staticMenuDesktopInactive: !this.layoutState().staticMenuDesktopInactive,
      }));
    } else {
      this.layoutState.update((prev) => ({
        ...prev,
        mobileMenuActive: !this.layoutState().mobileMenuActive,
      }));
    }
  }

  showConfigSidebar() {
    this.layoutState.update((prev) => ({ ...prev, configSidebarVisible: true }));
  }

  hideConfigSidebar() {
    this.layoutState.update((prev) => ({ ...prev, configSidebarVisible: false }));
  }

  isDesktop() {
    return window.innerWidth > 991;
  }

  isMobile() {
    return !this.isDesktop();
  }
}

const darkThemeKey = 'investmentlab.dark';

function systemDarkQuery(): MediaQueryList | undefined {
  return window.matchMedia?.('(prefers-color-scheme: dark)');
}

function storedDarkTheme(): boolean | null {
  try {
    const stored = localStorage.getItem(darkThemeKey);

    return stored === null ? null : stored === 'true';
  } catch {
    return null;
  }
}

function readDarkTheme(): boolean {
  const dark = storedDarkTheme() ?? systemDarkQuery()?.matches === true;

  document.documentElement.classList.toggle('app-dark', dark);

  return dark;
}

function writeDarkTheme(dark: boolean): void {
  if (dark === (systemDarkQuery()?.matches === true)) {
    clearDarkTheme();

    return;
  }

  try {
    localStorage.setItem(darkThemeKey, String(dark));
  } catch {}
}

function clearDarkTheme(): void {
  try {
    localStorage.removeItem(darkThemeKey);
  } catch {}
}

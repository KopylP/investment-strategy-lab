import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { Tooltip } from 'primeng/tooltip';
import { filter, map } from 'rxjs';
import { AuthStore } from '../auth/auth-store';
import { AccountContext, AccountScope } from '../context/account-context';
import { DisplayCurrency } from '../context/display-currency';
import { Viewport } from '../../shared/ui/viewport';
import { LayoutService } from './layout-service';
import { TapTooltip } from '../../shared/ui/tap-tooltip';

const allAccounts = 'all';

function scopeOf(route: ActivatedRouteSnapshot): AccountScope {
  let deepest = route;
  while (deepest.firstChild) deepest = deepest.firstChild;
  return (deepest.data['accountScope'] as AccountScope | undefined) ?? 'account';
}

@Component({
  selector: 'app-topbar',
  imports: [RouterLink, FormsModule, Select, SelectButton, Tooltip, TapTooltip, Menu],
  templateUrl: './app-topbar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppTopbar {
  protected readonly layout = inject(LayoutService);
  protected readonly accounts = inject(AccountContext);
  protected readonly display = inject(DisplayCurrency);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  protected readonly viewport = inject(Viewport);

  protected readonly email = computed(() => this.auth.user()?.email ?? '');

  /** What the open screen is about: one account, or optionally all accounts. */
  protected readonly scope = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => scopeOf(this.router.routerState.snapshot.root)),
    ),
    { initialValue: scopeOf(this.router.routerState.snapshot.root) },
  );

  protected readonly accountOptions = computed(() =>
    this.scope() === 'all' ? [{ id: allAccounts, name: 'Усі рахунки' }, ...this.accounts.accounts()] : this.accounts.accounts(),
  );

  protected readonly selectedAccount = computed(() => (this.scope() === 'all' && this.accounts.isAll() ? allAccounts : this.accounts.currentId()));

  /** The phone bar's title: the account the screen shows. */
  protected readonly accountLabel = computed(() => {
    if (this.selectedAccount() === allAccounts) return 'Усі рахунки';
    const account = this.accounts.current();
    return account ? `${account.name} · ${account.baseCurrency}` : 'Investment Lab';
  });

  protected readonly accountMenuItems = computed<MenuItem[]>(() =>
    this.accountOptions().map((account) => ({
      label: 'baseCurrency' in account ? `${account.name} · ${account.baseCurrency}` : account.name,
      icon: account.id === this.selectedAccount() ? 'pi pi-check' : 'pi pi-wallet',
      command: () => this.chooseAccount(account.id),
    })),
  );

  /**
   * On a phone there is no side menu (the sections are in the tab bar), so the settings and the theme are here —
   * the account button of Apple's own apps.
   */
  protected readonly userMenu = computed<MenuItem[]>(() => {
    const go = (path: string) => () => void this.router.navigate([path]);
    // Flat: in a p-menu with a group every top-level item becomes a group's heading.
    const phoneItems: MenuItem[] = [
      { label: 'Рахунки', icon: 'pi pi-wallet', command: go('/accounts') },
      { label: 'Стратегії', icon: 'pi pi-sliders-h', command: go('/strategies') },
      { label: 'Інструменти й котирування', icon: 'pi pi-chart-line', command: go('/instruments') },
      { label: 'Профіль', icon: 'pi pi-user', command: go('/profile') },
      { separator: true },
      { label: this.layout.isDarkTheme() ? 'Світла тема' : 'Темна тема', icon: this.layout.isDarkTheme() ? 'pi pi-sun' : 'pi pi-moon', command: () => this.toggleDarkMode() },
    ];
    return [
      ...(this.viewport.phone() ? phoneItems : [{ label: 'Профіль', icon: 'pi pi-user', command: go('/profile') }]),
      // In the Home Assistant panel Home Assistant signs the user in again at once.
      ...(this.auth.user()?.viaHomeAssistant ? [] : [{ separator: true }, { label: 'Вийти', icon: 'pi pi-sign-out', command: () => void this.signOut() }]),
    ];
  });

  protected toggleDarkMode(): void {
    this.layout.layoutConfig.update((config) => ({ ...config, darkTheme: !config.darkTheme }));
  }

  protected chooseCurrency(code: string): void {
    void this.display.choose(code);
  }

  protected chooseAccount(accountId: string): void {
    if (accountId === allAccounts) this.accounts.chooseAll();
    else this.accounts.choose(accountId);
  }

  private async signOut(): Promise<void> {
    await this.auth.logout();
    this.accounts.forget();
    await this.router.navigate(['/auth/login']);
  }
}

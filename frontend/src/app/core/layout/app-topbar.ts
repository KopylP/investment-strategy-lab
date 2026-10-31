import { ChangeDetectionStrategy, Component, computed, inject, viewChild } from '@angular/core';
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
  host: { '(window:scroll)': 'closeMenu()' },
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

  protected readonly userMenu = computed<MenuItem[]>(() => {
    const phone = this.viewport.phone();
    const accounts = this.accounts.accounts();
    const accountItems: MenuItem[] =
      phone && accounts.length > 1
        ? [
            {
              label: 'Рахунок',
              items: this.accountOptions().map((account) => ({
                label: 'baseCurrency' in account ? `${account.name} · ${account.baseCurrency}` : account.name,
                icon: account.id === this.selectedAccount() ? 'pi pi-check' : 'pi pi-wallet',
                command: () => this.chooseAccount(account.id),
              })),
            },
            { separator: true },
          ]
        : [];
    const themeItems: MenuItem[] = phone
      ? [{ label: this.layout.isDarkTheme() ? 'Світла тема' : 'Темна тема', icon: this.layout.isDarkTheme() ? 'pi pi-sun' : 'pi pi-moon', command: () => this.toggleDarkMode() }]
      : [];
    return [
      ...accountItems,
      { label: 'Профіль', icon: 'pi pi-user', command: () => void this.router.navigate(['/profile']) },
      ...themeItems,
      ...(this.auth.user()?.viaHomeAssistant ? [] : [{ separator: true }, { label: 'Вийти', icon: 'pi pi-sign-out', command: () => void this.signOut() }]),
    ];
  });

  private readonly menu = viewChild.required(Menu);

  protected closeMenu(): void {
    if (this.menu().visible) this.menu().hide();
  }

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

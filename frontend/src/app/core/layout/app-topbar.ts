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
import { LayoutService } from './layout-service';

const allAccounts = 'all';

function scopeOf(route: ActivatedRouteSnapshot): AccountScope {
  let deepest = route;
  while (deepest.firstChild) deepest = deepest.firstChild;
  return (deepest.data['accountScope'] as AccountScope | undefined) ?? 'account';
}

@Component({
  selector: 'app-topbar',
  imports: [RouterLink, FormsModule, Select, SelectButton, Tooltip, Menu],
  templateUrl: './app-topbar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppTopbar {
  protected readonly layout = inject(LayoutService);
  protected readonly accounts = inject(AccountContext);
  protected readonly display = inject(DisplayCurrency);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly email = computed(() => this.auth.user()?.email ?? '');

  /** What the open screen is about: one account, optionally all accounts, or the person. */
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

  protected readonly userMenu: MenuItem[] = [
    { label: 'Профіль', icon: 'pi pi-user', command: () => void this.router.navigate(['/profile']) },
    { separator: true },
    { label: 'Вийти', icon: 'pi pi-sign-out', command: () => void this.signOut() },
  ];

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

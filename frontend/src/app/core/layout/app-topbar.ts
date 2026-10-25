import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { Tooltip } from 'primeng/tooltip';
import { AuthStore } from '../auth/auth-store';
import { AccountContext } from '../context/account-context';
import { DisplayCurrency } from '../context/display-currency';
import { LayoutService } from './layout-service';

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
    this.accounts.choose(accountId);
  }

  private async signOut(): Promise<void> {
    await this.auth.logout();
    this.accounts.forget();
    await this.router.navigate(['/auth/login']);
  }
}

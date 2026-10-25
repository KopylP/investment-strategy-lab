import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { AuthStore } from '../auth/auth-store';
import { AccountContext } from '../context/account-context';
import { LayoutService } from './layout-service';

@Component({
  selector: 'app-topbar',
  imports: [RouterLink, FormsModule, Select, Menu],
  templateUrl: './app-topbar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppTopbar {
  protected readonly layout = inject(LayoutService);
  protected readonly accounts = inject(AccountContext);
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

  protected chooseAccount(accountId: string): void {
    this.accounts.choose(accountId);
  }

  private async signOut(): Promise<void> {
    await this.auth.logout();
    this.accounts.forget();
    await this.router.navigate(['/auth/login']);
  }
}

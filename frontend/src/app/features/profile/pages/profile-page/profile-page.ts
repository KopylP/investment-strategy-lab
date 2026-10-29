import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthStore } from '../../../../core/auth/auth-store';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { NotificationSettingsCard } from '../../ui/notification-settings/notification-settings';

/** The user's own settings. The display currency is switched in the top bar. */
@Component({
  selector: 'app-profile-page',
  imports: [PageHeader, NotificationSettingsCard],
  templateUrl: './profile-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  protected readonly auth = inject(AuthStore);
}

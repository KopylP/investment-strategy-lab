import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthStore } from '../../../../core/auth/auth-store';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignalNotificationSettings } from '../../../signals/ui/signal-notification-settings/signal-notification-settings';
import { ChartStyleSettingsCard } from '../../ui/chart-style-settings/chart-style-settings';
import { LanguageSettingsCard } from '../../ui/language-settings/language-settings';
import { NotificationSettingsCard } from '../../ui/notification-settings/notification-settings';

@Component({
  selector: 'app-profile-page',
  imports: [
    TranslocoPipe,
    PageHeader,
    LanguageSettingsCard,
    ChartStyleSettingsCard,
    NotificationSettingsCard,
    SignalNotificationSettings,
  ],
  templateUrl: './profile-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  protected readonly auth = inject(AuthStore);
}

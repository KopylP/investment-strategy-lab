import { ChangeDetectionStrategy, Component, inject, linkedSignal, resource, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { toProblemMessage } from '../../../../core/api/api-client';
import { AuthStore, UserProfile } from '../../../../core/auth/auth-store';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

@Component({
  selector: 'app-profile-page',
  imports: [FormsModule, Button, Select, Skeleton, PageHeader],
  templateUrl: './profile-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  protected readonly auth = inject(AuthStore);
  private readonly messages = inject(MessageService);

  private readonly profileResource = resource({ loader: () => this.auth.getProfile() });
  protected readonly profile = linkedSignal<UserProfile | undefined>(() => this.profileResource.value());
  protected readonly saving = signal(false);

  protected readonly currencies = ['EUR', 'USD', 'PLN'].map((code) => ({ label: code, value: code }));
  protected readonly locales = [{ label: 'Українська', value: 'uk-UA' }, { label: 'English', value: 'en-US' }];
  protected readonly timeZones = ['Europe/Warsaw', 'Europe/Kyiv', 'Europe/Berlin', 'Europe/London', 'America/New_York'].map((zone) => ({ label: zone, value: zone }));

  protected patch(change: Partial<UserProfile>): void {
    this.profile.update((profile) => (profile ? { ...profile, ...change } : profile));
  }

  protected async save(): Promise<void> {
    const profile = this.profile();
    if (!profile) return;
    this.saving.set(true);
    try {
      this.profileResource.set(await this.auth.updateProfile(profile));
      this.messages.add({ severity: 'success', summary: 'Профіль збережено' });
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не збережено', detail: toProblemMessage(error) });
    } finally {
      this.saving.set(false);
    }
  }
}

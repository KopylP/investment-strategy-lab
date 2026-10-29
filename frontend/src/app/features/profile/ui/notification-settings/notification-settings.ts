import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, resource, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { ApiClient, toProblemMessage } from '../../../../core/api/api-client';

export interface NotificationDestination {
  id: string;
  channel: string;
  label: string;
  /** The channel believes it is the user's own (Home Assistant: a tracker of their person). */
  suggested: boolean;
  selected: boolean;
}

export interface NotificationSettings {
  /** Empty outside Home Assistant: nothing is sent. */
  channels: string[];
  automatic: boolean;
  destinations: NotificationDestination[];
}

/** Where the user's report notifications are pushed, and a test notification to check it. */
@Component({
  selector: 'app-notification-settings',
  imports: [FormsModule, Button, Checkbox, Message, Skeleton, Tag],
  templateUrl: './notification-settings.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationSettingsCard {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);

  private readonly settingsResource = resource({ loader: () => this.api.get<NotificationSettings>('/api/notifications/settings') });
  protected readonly settings = linkedSignal<NotificationSettings | undefined>(() => this.settingsResource.value());
  protected readonly error = computed(() => (this.settingsResource.error() ? toProblemMessage(this.settingsResource.error()) : null));
  protected readonly saving = signal(false);
  protected readonly testing = signal(false);

  protected async toggle(destination: NotificationDestination, selected: boolean): Promise<void> {
    const current = this.settings();
    if (!current) return;
    const chosen = current.destinations.filter((each) => (each.id === destination.id ? selected : each.selected)).map((each) => each.id);
    await this.save(chosen);
  }

  protected backToAutomatic(): Promise<void> {
    return this.save(null);
  }

  protected async sendTest(): Promise<void> {
    this.testing.set(true);
    try {
      await this.api.post('/api/notifications/test');
      this.messages.add({ severity: 'success', summary: 'Тестове сповіщення надіслано', detail: 'Перевірте телефон і сповіщення в Home Assistant.' });
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не надіслано', detail: toProblemMessage(error) });
    } finally {
      this.testing.set(false);
    }
  }

  private async save(destinations: string[] | null): Promise<void> {
    this.saving.set(true);
    try {
      this.settingsResource.set(await this.api.put<NotificationSettings>('/api/notifications/settings', { destinations }));
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не збережено', detail: toProblemMessage(error) });
      this.settingsResource.reload();
    } finally {
      this.saving.set(false);
    }
  }
}

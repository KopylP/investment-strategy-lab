import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { MessageService } from 'primeng/api';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { toProblemMessage } from '../../../../core/api/api-client';
import { ChosenLanguage } from '../../../../core/i18n/chosen-language';
import { Language, languages } from '../../../../core/i18n/language';
import { translate } from '@jsverse/transloco';

const followBrowser = 'browser';

type LanguageOption = Language | typeof followBrowser;

@Component({
  selector: 'app-language-settings',
  imports: [FormsModule, TranslocoPipe, Select, Skeleton],
  templateUrl: './language-settings.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSettingsCard {
  private readonly language = inject(ChosenLanguage);
  private readonly messages = inject(MessageService);

  protected readonly saving = signal(false);
  protected readonly options: { value: LanguageOption; label: string }[] = [
    { value: followBrowser, label: translate('profile.language.followBrowser') },
    ...languages.map((code) => ({
      value: code,
      label: translate(`profile.language.names.${code}`),
    })),
  ];
  protected readonly selected = computed<LanguageOption | undefined>(() => {
    const choice = this.language.choice();

    return choice === undefined ? undefined : (choice ?? followBrowser);
  });

  protected async choose(option: LanguageOption): Promise<void> {
    this.saving.set(true);

    try {
      await this.language.choose(option === followBrowser ? null : option);
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: translate('profile.notSaved'),
        detail: toProblemMessage(error),
      });
    } finally {
      this.saving.set(false);
    }
  }
}

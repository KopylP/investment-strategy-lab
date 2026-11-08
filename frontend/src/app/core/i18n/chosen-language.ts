import { computed, DOCUMENT, effect, inject, Injectable, resource } from '@angular/core';
import { AuthStore } from '../auth/auth-store';
import {
  activeLanguage,
  isLanguage,
  Language,
  languageFor,
  rememberedLanguage,
  rememberLanguage,
} from './language';

@Injectable({ providedIn: 'root' })
export class ChosenLanguage {
  private readonly auth = inject(AuthStore);
  private readonly location = inject(DOCUMENT).location;

  private readonly profileResource = resource({
    params: () => (this.auth.isSignedIn() ? this.auth.user()?.id : undefined),
    loader: () => this.auth.getProfile(),
  });

  readonly choice = computed<Language | null | undefined>(() => {
    const profile = this.profileResource.value();
    if (profile === undefined) return undefined;

    return isLanguage(profile.language) ? profile.language : null;
  });

  constructor() {
    effect(() => {
      const choice = this.choice();

      if (choice !== undefined && choice !== rememberedLanguage()) this.apply(choice);
    });
  }

  async choose(choice: Language | null): Promise<void> {
    const profile = await this.auth.getProfile();
    const saved = await this.auth.updateProfile({ ...profile, language: choice });

    this.profileResource.set(saved);
    this.apply(choice);
  }

  private apply(choice: Language | null): void {
    rememberLanguage(choice);

    if (languageFor(choice) !== activeLanguage) this.location.reload();
  }
}

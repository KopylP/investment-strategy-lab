import { HttpErrorResponse } from '@angular/common/http';
import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { ApiSchema } from '../api/api-schema';
import { isInHomeAssistantPanel } from '../home-assistant/home-assistant-panel';

export type CurrentUser = ApiSchema<'CurrentUserDto'>;
export type UserProfile = ApiSchema<'UserProfileDto'>;
type LoginBody = ApiSchema<'LoginCommand'>;
type RegisterBody = ApiSchema<'RegisterCommand'>;
type UpdateProfileBody = ApiSchema<'UpdateProfileCommand'>;

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(ApiClient);
  private readonly userState = signal<CurrentUser | null | undefined>(undefined);

  readonly user = this.userState.asReadonly();
  readonly isSignedIn = computed(() => !!this.userState());

  readonly inHomeAssistantPanel = isInHomeAssistantPanel(inject(DOCUMENT));

  async ensureLoaded(): Promise<CurrentUser | null> {
    if (this.userState() !== undefined) return this.userState() ?? null;

    try {
      this.userState.set(await this.api.get<CurrentUser>('/api/identity/me'));
    } catch (error) {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) throw error;

      this.userState.set(null);
    }

    return this.userState() ?? null;
  }

  async login(email: string, password: string): Promise<void> {
    const body: LoginBody = { email, password, remember: true };

    await this.api.post('/api/identity/login', body);
    this.userState.set(undefined);
    await this.ensureLoaded();
  }

  async register(email: string, password: string): Promise<void> {
    const body: RegisterBody = { email, password };

    await this.api.post<CurrentUser>('/api/identity/register', body);
    await this.login(email, password);
  }

  async logout(): Promise<void> {
    try {
      await this.api.post('/api/identity/logout');
    } finally {
      this.userState.set(null);
    }
  }

  signedOut(): void {
    this.userState.set(null);
  }

  getProfile(): Promise<UserProfile> {
    return this.api.get<UserProfile>('/api/identity/profile');
  }

  updateProfile(profile: UpdateProfileBody): Promise<UserProfile> {
    return this.api.put<UserProfile>('/api/identity/profile', profile);
  }
}

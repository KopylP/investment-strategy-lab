import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { StrategySetupInput } from '../models/strategy';
import { StrategiesApi } from './strategies-api';

@Injectable()
export class StrategiesStore {
  private readonly api = inject(StrategiesApi);
  private readonly messages = inject(MessageService);
  readonly instruments = inject(InstrumentCatalog);

  private readonly listResource = resource({ loader: () => this.api.list() });
  private readonly catalogResource = resource({ loader: () => this.api.catalog() });

  readonly strategies = computed(() => this.listResource.value() ?? []);
  readonly isLoading = this.listResource.isLoading;
  readonly loadError = this.listResource.error;
  readonly types = computed(() => this.catalogResource.value() ?? []);
  readonly creatingDefaults = signal(false);

  typeName(code: string): string {
    return this.types().find((type) => type.code === code)?.name ?? code;
  }

  async createDefaults(): Promise<void> {
    this.creatingDefaults.set(true);
    try {
      const created = await this.api.createDefaults();
      this.messages.add({
        severity: 'success',
        summary: created.length ? 'Стратегії створено' : 'Стандартні стратегії вже є',
        detail: created.map((strategy) => strategy.name).join(', '),
      });
      this.listResource.reload();
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: 'Не вдалося створити стратегії',
        detail: toProblemMessage(error),
        life: 8000,
      });
    } finally {
      this.creatingDefaults.set(false);
    }
  }
}

@Injectable()
export class StrategyDetailStore {
  private readonly api = inject(StrategiesApi);
  private readonly messages = inject(MessageService);
  readonly instruments = inject(InstrumentCatalog);

  private readonly id = signal<string | undefined>(undefined);
  private readonly versionNumber = signal<number | undefined>(undefined);

  private readonly strategyResource = resource({
    params: () => (this.id() ? { id: this.id()!, version: this.versionNumber() } : undefined),
    loader: ({ params }) => this.api.get(params.id, params.version),
  });
  private readonly catalogResource = resource({ loader: () => this.api.catalog() });

  readonly strategy = computed(() =>
    this.strategyResource.hasValue() ? this.strategyResource.value() : undefined,
  );
  readonly isLoading = this.strategyResource.isLoading;
  readonly loadError = this.strategyResource.error;
  readonly type = computed(() =>
    this.catalogResource.value()?.find((type) => type.code === this.strategy()?.typeCode),
  );
  readonly versions = computed(() =>
    Array.from({ length: this.strategy()?.latestVersion ?? 0 }, (_, index) => index + 1).reverse(),
  );
  readonly saving = signal(false);

  open(id: string): void {
    this.id.set(id);
    this.versionNumber.set(undefined);
  }

  showVersion(version: number): void {
    this.versionNumber.set(version);
  }

  async save(setup: StrategySetupInput): Promise<boolean> {
    const id = this.id();
    if (!id) return false;
    this.saving.set(true);
    try {
      const saved = await this.api.reconfigure(id, setup);
      this.messages.add({
        severity: 'success',
        summary: `Збережено як версію ${saved.latestVersion}`,
        detail:
          'Під-портфелі, що йдуть за останньою версією, використовують її з наступного рапорту',
      });
      this.versionNumber.set(undefined);
      this.strategyResource.reload();
      return true;
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: 'Параметри не збережено',
        detail: toProblemMessage(error),
        life: 10000,
      });
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  async archive(): Promise<void> {
    const id = this.id();
    if (!id) return;
    try {
      await this.api.archive(id);
      this.messages.add({ severity: 'success', summary: 'Стратегію архівовано' });
      this.strategyResource.reload();
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: 'Не вдалося архівувати',
        detail: toProblemMessage(error),
      });
    }
  }
}

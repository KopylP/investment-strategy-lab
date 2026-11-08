import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { StrategyBuilderApi } from '../../data-access/strategy-builder-api';
import { FundSearchStore } from '../../data-access/fund-search-store';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { FoundFundDto } from '../../models/found-fund-dto';
import { InstrumentPickerVm } from '../../models/instrument-picker-vm';
import { fundPicker } from '../../data-access/setup-pickers';
import { toProblemMessage } from '../../../../core/api/api-client';
import { activeLocale } from '../../../../core/i18n/language';

@Component({
  selector: 'app-fund-selection-dialog',
  imports: [FormsModule, Button, Dialog, Select, InputText, InputNumber, SelectButton],
  providers: [FundSearchStore],
  templateUrl: './fund-selection-dialog.html',
  styleUrl: './fund-selection-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FundSelectionDialog {
  readonly visible = model(false);
  readonly picker = input<InstrumentPickerVm>(fundPicker);
  readonly instruments = input.required<InstrumentChoiceDto[]>();
  readonly value = input.required<string[]>();
  readonly overlayContainer = input<HTMLElement | 'body'>('body');
  readonly applied = output<string[]>();
  readonly instrumentsChanged = output<void>();
  private readonly dialog = viewChild.required(Dialog);
  protected readonly store = inject(FundSearchStore);
  private readonly api = inject(StrategyBuilderApi);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly pending = signal<FoundFundDto[]>([]);
  protected readonly query = signal('');
  protected readonly remoteQuery = signal('');
  protected readonly scope = signal<'added' | 'external'>('added');
  protected readonly searchScopes = [
    { label: 'Додані інструменти', value: 'added' },
    { label: 'Пошук у джерелі котирувань', value: 'external' },
  ];
  protected readonly exchange = signal<string | null>(null);
  protected readonly region = signal<string | null>(null);
  protected readonly family = signal('');
  protected readonly minAssets = signal<number | null>(null);
  protected readonly maxFee = signal<number | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly failures = signal<{ symbol: string; reason: string }[]>([]);
  protected readonly single = computed(() => this.picker().limit === 1);
  protected readonly full = computed(() => !this.single() && this.count() >= this.picker().limit);
  protected readonly count = computed(() => this.selectedIds().length + this.pending().length);
  protected readonly selected = computed(() => [
    ...this.selectedIds().map((id) => {
      const item = this.instruments().find((item) => item.id === id);
      return { key: id, label: item?.label ?? 'Фонд', name: item?.name ?? id, pending: false };
    }),
    ...this.pending().map((item) => ({
      key: item.symbol,
      label: item.symbol,
      name: item.name,
      pending: true,
    })),
  ]);
  protected readonly groups = computed(() => {
    const query = this.query().trim().toLocaleLowerCase();
    const groups = new Map<string, InstrumentChoiceDto[]>();
    for (const item of this.instruments()) {
      if (!this.fits(item, query)) continue;
      const group = item.group ?? 'Інші';
      groups.set(group, [...(groups.get(group) ?? []), item]);
    }
    return [...groups]
      .sort(([a], [b]) => a.localeCompare(b, activeLocale))
      .map(([label, items]) => ({ label, items }));
  });
  protected readonly external = computed(() => this.store.funds() ?? []);

  constructor() {
    effect(() => {
      if (this.visible())
        untracked(() => {
          this.selectedIds.set([...this.value()]);
          this.pending.set([]);
          this.query.set('');
          this.remoteQuery.set('');
          this.scope.set('added');
          this.error.set(null);
          this.failures.set([]);
          this.store.funds.set(null);
          void this.store.loadOptions();
        });
    });
  }
  private fits(item: InstrumentChoiceDto, query: string): boolean {
    const searchable = `${item.label} ${item.name}`.toLocaleLowerCase();

    return item.isVolatilityIndex === this.picker().volatilityIndices && searchable.includes(query);
  }
  protected keepInFullscreen(): void {
    const fullscreen = document.fullscreenElement;
    const wrapper = this.dialog().wrapper;
    if (fullscreen && wrapper && !fullscreen.contains(wrapper)) fullscreen.appendChild(wrapper);
  }
  protected has(fund: FoundFundDto): boolean {
    return fund.instrumentId
      ? this.selectedIds().includes(fund.instrumentId)
      : this.pending().some((item) => item.symbol === fund.symbol);
  }
  protected toggleId(id: string): void {
    if (this.busy()) return;
    if (this.single()) return this.chooseOnly([id], []);
    if (this.selectedIds().includes(id))
      this.selectedIds.update((ids) => ids.filter((value) => value !== id));
    else if (this.canAdd(1)) this.selectedIds.update((ids) => [...ids, id]);
  }
  protected toggleExternal(fund: FoundFundDto): void {
    if (fund.instrumentId) return this.toggleId(fund.instrumentId);
    if (this.busy()) return;
    if (this.single()) return this.chooseOnly([], [fund]);
    if (this.has(fund))
      this.pending.update((items) => items.filter((item) => item.symbol !== fund.symbol));
    else if (this.canAdd(1)) this.pending.update((items) => [...items, fund]);
  }
  protected readonly hasSelectedId = (item: InstrumentChoiceDto) =>
    this.selectedIds().includes(item.id);
  protected groupSelected(items: InstrumentChoiceDto[]): boolean {
    return items.every((item) => this.selectedIds().includes(item.id));
  }
  protected toggleGroup(items: InstrumentChoiceDto[]): void {
    if (this.busy()) return;
    const ids = new Set(items.map((item) => item.id));
    if (this.groupSelected(items))
      this.selectedIds.update((selected) => selected.filter((id) => !ids.has(id)));
    else {
      const missing = items.filter((item) => !this.selectedIds().includes(item.id));
      if (this.canAdd(missing.length))
        this.selectedIds.update((selected) => [...selected, ...missing.map((item) => item.id)]);
    }
  }
  protected remove(key: string, pending: boolean): void {
    if (pending) this.pending.update((items) => items.filter((item) => item.symbol !== key));
    else this.selectedIds.update((ids) => ids.filter((id) => id !== key));
  }
  private chooseOnly(ids: string[], pending: FoundFundDto[]): void {
    this.selectedIds.set(ids);
    this.pending.set(pending);
  }
  private canAdd(number: number): boolean {
    const limit = this.picker().limit;
    if (this.count() + number <= limit) {
      this.error.set(null);
      return true;
    }
    this.error.set(
      `Можна вибрати до ${limit}. Зніміть частину вибору або виберіть окремі інструменти.`,
    );
    return false;
  }
  protected search(): void {
    if (this.busy() || this.store.searching()) return;
    void this.store.search({
      exchangeId: this.exchange(),
      regionId: this.region(),
      fundFamily: this.family().trim() || null,
      minAssets: this.minAssets(),
      maxExpenseRatio: this.maxFee() === null ? null : this.maxFee()! / 100,
      nameWords: this.remoteQuery().trim().split(/\s+/u).filter(Boolean),
    });
  }
  protected async apply(): Promise<void> {
    if (this.busy() || this.count() > this.picker().limit) return;
    if (!this.pending().length) {
      this.applied.emit([...this.selectedIds()]);
      this.visible.set(false);
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.failures.set([]);
    try {
      const funds = this.pending();
      const result = await this.api.addFunds(funds, this.selectedIds());
      this.selectedIds.update((ids) => [...new Set([...ids, ...result.instrumentIds])]);
      const failed = new Set(result.failed.map((item) => item.symbol));
      this.pending.set(funds.filter((item) => failed.has(item.symbol)));
      this.failures.set(result.failed);
      this.instrumentsChanged.emit();
      if (!result.failed.length) {
        this.applied.emit([...this.selectedIds()]);
        this.visible.set(false);
      }
    } catch (error) {
      this.error.set(toProblemMessage(error));
    } finally {
      this.busy.set(false);
    }
  }
}

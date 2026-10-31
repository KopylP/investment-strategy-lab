import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  model,
  signal,
  untracked,
} from '@angular/core';
import { StrategyPicker } from '../../../strategies/ui/strategy-picker/strategy-picker';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { symbolOf } from '../../../../shared/models/api';
import { AccountDetailStore } from '../../data-access/account-detail-store';

interface SubPortfolioDraft {
  name: string;
  share: number;
  strategyId: string | null;
  cashInstrumentId: string | null;
}

@Component({
  selector: 'app-add-sub-portfolio-dialog',
  imports: [StrategyPicker, FormsModule, Button, Dialog, InputNumber, InputText, Select],
  templateUrl: './add-sub-portfolio-dialog.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddSubPortfolioDialog {
  protected readonly store = inject(AccountDetailStore);
  private readonly instruments = inject(InstrumentCatalog);

  readonly visible = model(false);

  protected readonly draft = signal<SubPortfolioDraft>({
    name: '',
    share: 10,
    strategyId: null,
    cashInstrumentId: null,
  });

  protected readonly cashOptions = computed(() =>
    this.instruments
      .etfs()
      .map((etf) => ({ label: `${symbolOf(etf)} · ${etf.name}`, value: etf.id })),
  );

  constructor() {
    effect(() => {
      if (this.visible()) untracked(() => this.reset());
    });
  }

  private reset(): void {
    this.draft.set({ name: '', share: 10, strategyId: null, cashInstrumentId: null });
  }

  protected patchDraft(change: Partial<SubPortfolioDraft>): void {
    this.draft.update((draft) => ({ ...draft, ...change }));
  }

  protected async add(): Promise<void> {
    const draft = this.draft();
    const added = await this.store.addSubPortfolio({
      name: draft.name.trim(),
      targetSharePercent: draft.share,
      strategyId: draft.strategyId,
      cashInstrumentId: draft.cashInstrumentId,
      maxPositionWeightPercent: null,
    });
    if (added) this.visible.set(false);
  }
}

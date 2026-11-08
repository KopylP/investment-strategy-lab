import { TranslocoPipe } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  model,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { SubPortfolioSetup } from '../../../../shared/models/api';
import { AccountDetailStore } from '../../data-access/account-detail-store';

@Component({
  selector: 'app-rename-sub-portfolio-dialog',
  imports: [TranslocoPipe, FormsModule, Button, Dialog, InputText],
  templateUrl: './rename-sub-portfolio-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RenameSubPortfolioDialog {
  readonly subPortfolio = model<SubPortfolioSetup | null>(null);

  protected readonly store = inject(AccountDetailStore);
  protected readonly name = linkedSignal(() => this.subPortfolio()?.name ?? '');
  protected readonly canSave = computed(() => {
    const name = this.name().trim();

    return !!name && name !== this.subPortfolio()?.name;
  });

  protected async save(): Promise<void> {
    const subPortfolio = this.subPortfolio();
    if (!subPortfolio || !this.canSave()) return;

    const saved = await this.store.renameSubPortfolio(subPortfolio.id, this.name().trim());
    if (saved) this.subPortfolio.set(null);
  }
}

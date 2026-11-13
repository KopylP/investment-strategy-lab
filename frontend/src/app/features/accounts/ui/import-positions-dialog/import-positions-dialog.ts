import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  model,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { FileDrop } from '../../../../shared/ui/file-drop';
import { toIsoDate } from '../../../../shared/models/iso-date';
import { AccountDetailStore } from '../../data-access/account-detail-store';

@Component({
  selector: 'app-import-positions-dialog',
  imports: [TranslocoPipe, FormsModule, Button, DatePicker, Dialog, Message, Select, FileDrop],
  templateUrl: './import-positions-dialog.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportPositionsDialog {
  protected readonly store = inject(AccountDetailStore);

  readonly visible = model(false);

  protected readonly importSleeve = signal<string | null>(null);
  protected readonly importDate = signal<Date>(new Date());
  protected readonly importJson = signal<unknown>(null);
  protected readonly importFileName = signal('');
  protected readonly importError = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (this.visible()) untracked(() => this.reset());
    });
  }

  private reset(): void {
    this.importSleeve.set(this.store.active()[0]?.id ?? null);
    this.importJson.set(null);
    this.importFileName.set('');
    this.importError.set(null);
  }

  protected async readFile(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.importFileName.set(file.name);

    try {
      const json = JSON.parse(await file.text()) as { positions?: unknown[] };
      if (!Array.isArray(json.positions))
        throw new Error(translate('accounts.importPositions.noPositions'));

      this.importJson.set(json);
      this.importError.set(null);
    } catch (error) {
      this.importJson.set(null);
      this.importError.set(
        error instanceof Error ? error.message : translate('accounts.importPositions.notJson'),
      );
    }
  }

  protected async runImport(): Promise<void> {
    const sleeve = this.importSleeve();
    const json = this.importJson();
    if (!sleeve || !json) return;

    if (await this.store.importPositions(sleeve, json, toIsoDate(this.importDate())))
      this.visible.set(false);
  }
}

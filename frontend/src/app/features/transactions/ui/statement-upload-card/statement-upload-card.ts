import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Select } from 'primeng/select';
import { FileDrop } from '../../../../shared/ui/file-drop';
import { StatementImportStore } from '../../data-access/statement-import-store';

@Component({
  selector: 'app-statement-upload-card',
  imports: [FormsModule, RouterLink, Select, FileDrop],
  templateUrl: './statement-upload-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatementUploadCard {
  protected readonly store = inject(StatementImportStore);

  protected async readFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) await this.store.upload(file);

    input.value = '';
  }
}

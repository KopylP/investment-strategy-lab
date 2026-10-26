import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { DatePipe, MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { DeclarationField, formValue, TaxDeclaration } from '../../data-access/declarations';

/** The filled return, field by field with its "poz." number, each value one click from the clipboard. */
@Component({
  selector: 'app-declaration-dialog',
  imports: [NgTemplateOutlet, Dialog, Button, Message, Skeleton, MoneyPipe, DatePipe],
  templateUrl: './declaration-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeclarationDialog {
  private readonly messages = inject(MessageService);

  readonly declaration = input<TaxDeclaration | null>(null);
  readonly warnings = input<string[]>([]);
  readonly loading = input(false);
  readonly visible = input(false);
  readonly pdfLoading = input(false);
  readonly closed = output<void>();
  readonly pdf = output<void>();

  protected readonly formValue = formValue;

  protected async copy(field: DeclarationField): Promise<void> {
    await this.write(formValue(field), `poz. ${field.position} скопійовано`);
  }

  protected async copyAll(declaration: TaxDeclaration): Promise<void> {
    const lines = [
      `${declaration.form} за ${declaration.year}`,
      ...declaration.sections.flatMap((section) => [`${section.code}. ${section.title}`, ...section.fields.map((field) => `  poz. ${field.position} ${field.label}: ${formValue(field)}`)]),
      ...declaration.attachments.flatMap((attachment) => [attachment.title, ...attachment.fields.map((field) => `  poz. ${field.position} ${field.label}: ${formValue(field)}`)]),
    ];
    await this.write(lines.join('\n'), 'Усі позиції скопійовано');
  }

  private async write(text: string, summary: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      this.messages.add({ severity: 'success', summary, life: 1500 });
    } catch {
      this.messages.add({ severity: 'warn', summary: 'Не вдалося скопіювати', detail: text });
    }
  }
}

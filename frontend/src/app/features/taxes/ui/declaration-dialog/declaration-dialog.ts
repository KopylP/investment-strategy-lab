import { TranslocoPipe, translate } from '@jsverse/transloco';
import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { DatePipe, MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { formValue } from '../../data-access/declarations';
import {
  DeclarationAccountLine,
  DeclarationField,
  Pit8CAmounts,
  TaxDeclaration,
} from '../../models/declaration-dto';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';

@Component({
  selector: 'app-declaration-dialog',
  imports: [
    TranslocoPipe,
    TaxMoneyPipe,
    NgTemplateOutlet,
    FormsModule,
    InputNumber,
    Dialog,
    Button,
    Message,
    Skeleton,
    Tooltip,
    TapTooltip,
    MoneyPipe,
    DatePipe,
  ],
  templateUrl: './declaration-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeclarationDialog {
  private readonly messages = inject(MessageService);

  readonly declaration = input<TaxDeclaration | null>(null);
  readonly warnings = input<string[]>([]);
  readonly loading = input(false);
  readonly visible = input(false);
  readonly pdfLoading = input<'summary' | 'form' | null>(null);
  readonly problem = input<string | null>(null);
  readonly saving = input(false);
  readonly closed = output<void>();
  readonly pdf = output<'summary' | 'form'>();
  readonly pit8c = output<{ accountId: string; amounts: Pit8CAmounts }>();
  readonly pit8cRemoved = output<string>();

  protected readonly editing = signal<{
    accountId: string;
    revenue: number | null;
    costs: number | null;
  } | null>(null);

  protected edit(account: DeclarationAccountLine): void {
    this.editing.set({
      accountId: account.accountId,
      revenue: account.pit8C?.revenue ?? null,
      costs: account.pit8C?.costs ?? null,
    });
  }

  protected save(): void {
    const draft = this.editing();
    if (!draft || draft.revenue === null || draft.costs === null) return;

    this.pit8c.emit({
      accountId: draft.accountId,
      amounts: { revenue: draft.revenue, costs: draft.costs },
    });
    this.editing.set(null);
  }

  protected readonly formValue = formValue;

  protected async copy(field: DeclarationField): Promise<void> {
    await this.write(
      formValue(field),
      translate('taxes.declaration.fieldCopied', { position: field.position }),
    );
  }

  protected async copyAll(declaration: TaxDeclaration): Promise<void> {
    const lines = [
      translate('taxes.formOfYear', { form: declaration.form, year: declaration.year }),
      ...declaration.sections.flatMap((section) => [
        `${section.code}. ${section.title}`,
        ...section.fields.map(
          (field) => `  poz. ${field.position} ${field.label}: ${formValue(field)}`,
        ),
      ]),
      ...declaration.attachments.flatMap((attachment) => [
        attachment.title,
        ...attachment.fields.map(
          (field) => `  poz. ${field.position} ${field.label}: ${formValue(field)}`,
        ),
      ]),
    ];

    await this.write(lines.join('\n'), translate('taxes.declaration.copiedAll'));
  }

  private async write(text: string, summary: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      this.messages.add({ severity: 'success', summary, life: 1500 });
    } catch {
      this.messages.add({
        severity: 'warn',
        summary: translate('taxes.declaration.copyFailed'),
        detail: text,
      });
    }
  }
}

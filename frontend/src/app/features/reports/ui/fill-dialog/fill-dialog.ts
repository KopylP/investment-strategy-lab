import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { Textarea } from 'primeng/textarea';
import { toIsoDate } from '../../../../shared/models/api';
import { MoneyPipe, QuantityPipe } from '../../../../shared/pipes/format-pipes';
import { FillInput, ReportActionVm } from '../../models/report-vm';

export interface FillDialogData {
  action: ReportActionVm;
  mode: 'full' | 'partial';
}

/** What was really done: defaults are the plan, the user corrects quantity, price, fee and date. */
@Component({
  selector: 'app-fill-dialog',
  imports: [ReactiveFormsModule, Button, DatePicker, InputNumber, Textarea, Message, MoneyPipe, QuantityPipe],
  templateUrl: './fill-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FillDialog {
  private readonly ref = inject(DynamicDialogRef);
  protected readonly data = inject(DynamicDialogConfig<FillDialogData>).data as FillDialogData;
  protected readonly action = this.data.action;
  protected readonly partial = this.data.mode === 'partial';

  protected readonly form = inject(NonNullableFormBuilder).group({
    quantity: [this.partial ? 0 : this.defaultQuantity(), [Validators.required, Validators.min(0.00000001)]],
    price: [this.action.isMoney ? 1 : this.action.plannedPrice, [Validators.required, Validators.min(0.00000001)]],
    fee: [0, [Validators.required, Validators.min(0)]],
    executedOn: [new Date(), Validators.required],
    note: [''],
  });

  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly total = computed(() => (this.values().quantity ?? 0) * (this.values().price ?? 0));
  protected readonly tooMuch = computed(() => this.partial && (this.values().quantity ?? 0) >= this.action.remainingQuantity);
  protected readonly submitted = signal(false);
  protected readonly today = new Date();

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid || this.tooMuch()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const fill: FillInput = {
      quantity: value.quantity,
      price: this.action.isMoney ? 1 : value.price,
      fee: value.fee,
      executedOn: toIsoDate(value.executedOn),
      note: value.note.trim() || null,
    };
    this.ref.close(fill);
  }

  protected cancel(): void {
    this.ref.close();
  }

  private defaultQuantity(): number {
    return this.action.remainingQuantity > 0 ? this.action.remainingQuantity : this.action.plannedQuantity;
  }
}

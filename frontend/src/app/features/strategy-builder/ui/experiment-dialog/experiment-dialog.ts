import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { defaultExperimentForm, experimentFromForm, valuesAreNumbers } from '../../data-access/experiment-form-mapper';
import { ExperimentFormVm } from '../../models/experiment-form-vm';
import { ExperimentParameterVm } from '../../models/experiment-parameter-vm';
import { ExperimentRequestVm } from '../../models/experiment-request-vm';
import { activeLocale } from '../../../../core/i18n/language';

const storageKey = 'strategy-builder.experiment-form';

@Component({
  selector: 'app-experiment-dialog',
  imports: [TranslocoPipe, FormsModule, Button, Checkbox, Dialog, InputNumber, InputText, Message, Select],
  templateUrl: './experiment-dialog.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExperimentDialog {
  readonly visible = model(false);
  readonly parameters = input.required<ExperimentParameterVm[]>();
  readonly busy = input(false);
  readonly error = input<string | null>(null);
  readonly requested = output<ExperimentRequestVm>();
  protected readonly form = signal(this.restore());
  protected readonly parameter = computed(() => this.parameters().find((choice) => choice.path === this.form().parameterPath) ?? null);
  protected readonly canSubmit = computed(() => this.validForm());
  protected readonly currentParameter = computed(() => {
    const parameter = this.parameter();
    if (!parameter) return '';
    const value = parameter.percent ? parameter.currentValue * 100 : parameter.currentValue;

    return value.toLocaleString(activeLocale, { maximumFractionDigits: 6 }) + (parameter.percent ? ' %' : '');
  });

  protected patch(change: Partial<ExperimentFormVm>): void {
    this.form.update((current) => ({ ...current, ...change }));
  }

  protected submit(): void {
    if (!this.canSubmit() || this.busy()) return;
    const form = this.form();
    const experiment = experimentFromForm(form, this.parameter());
    this.remember(form);
    this.requested.emit({ label: form.label.trim() || null, experiment });
  }

  private validForm(): boolean {
    const form = this.form();
    if (!form.start || !form.end) return false;
    if (form.independent && !form.outOfSampleStart) return false;

    return !form.sensitivity || (this.parameter() !== null && valuesAreNumbers(form.parameterValues));
  }

  private restore(): ExperimentFormVm {
    const defaults = defaultExperimentForm();
    try {
      const saved = localStorage.getItem(storageKey);
      if (!saved) return defaults;
      const parsed = JSON.parse(saved) as Partial<ExperimentFormVm>;

      return { ...defaults, ...parsed, label: '' };
    } catch {
      return defaults;
    }
  }

  private remember(form: ExperimentFormVm): void {
    try {
      localStorage.setItem(storageKey, JSON.stringify(form));
    } catch {
      return;
    }
  }
}

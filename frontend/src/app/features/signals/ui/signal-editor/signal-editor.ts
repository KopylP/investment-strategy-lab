import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { symbolOf } from '../../../../shared/models/api';
import { ReportTiming } from '../../../reports/models/report-schedule';
import { ReportTimingEditor } from '../../../reports/ui/report-timing-editor/report-timing-editor';
import { SignalsStore } from '../../data-access/signals-store';
import {
  SignalDto,
  SignalMetric,
  SignalReportRuleDto,
  SignalTargetKind,
} from '../../models/signal-dto';
import { emptyForm, formOf, isFormComplete, metricsOf, SignalForm } from '../../models/signal-form';
import { metricLabels, targetLabels } from '../../models/signal-labels';

const signalFrequencyOptions = [
  { label: 'Після оновлення даних', value: 'off' },
  { label: 'Щогодини', value: 'hourly' },
  { label: 'Щодня', value: 'daily' },
  { label: 'Щотижня', value: 'weekly' },
];

@Component({
  selector: 'app-signal-editor',
  imports: [
    FormsModule,
    Button,
    Dialog,
    InputNumber,
    InputText,
    Message,
    Select,
    ToggleSwitch,
    ReportTimingEditor,
  ],
  templateUrl: './signal-editor.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignalEditor {
  protected readonly store = inject(SignalsStore);
  private readonly confirmation = inject(ConfirmationService);

  readonly visible = model(false);
  readonly editedSignal = input<SignalDto | null>(null);

  protected readonly form = signal<SignalForm>(emptyForm(null));
  protected readonly ruleAccountId = signal<string | null>(null);
  protected readonly ruleReportCode = signal<string | null>(null);

  protected readonly frequencies = signalFrequencyOptions;
  protected readonly directions = [
    { label: 'Вище порога', value: 'Above' },
    { label: 'Нижче порога', value: 'Below' },
  ];
  protected readonly targets = Object.entries(targetLabels).map(([value, label]) => ({
    label,
    value: value as SignalTargetKind,
  }));

  protected readonly isEditing = computed(() => this.editedSignal() !== null);
  protected readonly isComplete = computed(() => isFormComplete(this.form()));
  protected readonly isLevel = computed(() => this.form().metric === 'Level');
  protected readonly unit = computed(() => (this.isLevel() ? '' : '%'));

  protected readonly metrics = computed(() =>
    metricsOf(this.form().target).map((value) => ({ label: metricLabels[value], value })),
  );
  protected readonly accounts = computed(() =>
    this.store.accounts().map((account) => ({ label: account.name, value: account.id })),
  );
  protected readonly subPortfolios = computed(() =>
    this.subPortfolioOptions(this.form().accountId),
  );
  protected readonly instruments = computed(() =>
    this.store.instruments().map((instrument) => ({
      label: `${symbolOf(instrument)} · ${instrument.name}`,
      value: instrument.id,
    })),
  );

  protected readonly rules = computed(() => this.rulesOfCurrentSignal());
  protected readonly ruleReports = computed(() =>
    this.store
      .reportsOf(this.ruleAccountId())
      .map((report) => ({ label: report.name, value: report.code })),
  );

  constructor() {
    effect(() => {
      if (this.visible()) untracked(() => this.resetForm());
    });
  }

  protected patch(part: Partial<SignalForm>): void {
    this.form.update((current) => ({ ...current, ...part }));
  }

  protected changeTarget(target: SignalTargetKind): void {
    const metric = this.metricFitting(target);

    this.patch({ target, metric });
  }

  protected changeSchedule(schedule: ReportTiming): void {
    this.patch({ schedule });
  }

  protected async save(): Promise<void> {
    const current = this.editedSignal();
    const saved = current
      ? await this.store.change(current.id, this.form())
      : await this.store.create(this.form());

    if (saved) this.visible.set(false);
  }

  protected confirmDelete(signalId: string): void {
    this.confirmation.confirm({
      header: 'Видалити сигнал?',
      message: 'Разом із ним зникне його історія, а під’єднані рапорти більше не запускатимуться.',
      icon: 'pi pi-trash',
      acceptLabel: 'Видалити',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.delete(signalId),
    });
  }

  protected async addRule(signalId: string): Promise<void> {
    const accountId = this.ruleAccountId();
    const reportCode = this.ruleReportCode();
    if (!accountId || !reportCode) return;

    const added = await this.store.addReportRule(signalId, accountId, reportCode);

    if (added) this.ruleReportCode.set(null);
  }

  private async delete(signalId: string): Promise<void> {
    const deleted = await this.store.delete(signalId);

    if (deleted) this.visible.set(false);
  }

  private resetForm(): void {
    const current = this.editedSignal();
    const firstAccountId = this.store.accounts()[0]?.id ?? null;

    this.form.set(current ? formOf(current) : emptyForm(firstAccountId));
    this.ruleAccountId.set(current?.accountId ?? firstAccountId);
    this.ruleReportCode.set(null);
  }

  private metricFitting(target: SignalTargetKind): SignalMetric {
    const allowed = metricsOf(target);
    const current = this.form().metric;

    return allowed.includes(current) ? current : allowed[0];
  }

  private subPortfolioOptions(accountId: string | null): { label: string; value: string }[] {
    const account = this.store.accounts().find((each) => each.id === accountId);

    return (account?.subPortfolios ?? []).map((sub) => ({ label: sub.name, value: sub.id }));
  }

  private rulesOfCurrentSignal(): SignalReportRuleDto[] {
    const current = this.editedSignal();

    return current ? this.store.rulesOf(current.id) : [];
  }
}

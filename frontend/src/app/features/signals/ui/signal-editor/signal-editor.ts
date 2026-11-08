import { TranslocoPipe, translate } from '@jsverse/transloco';
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
import { ReportTiming } from '../../../reports/models/report-schedule';
import { ReportTimingEditor } from '../../../reports/ui/report-timing-editor/report-timing-editor';
import { SignalsStore } from '../../data-access/signals-store';
import {
  SignalDto,
  SignalMetric,
  SignalReportRuleDto,
  SignalTargetKind,
} from '../../models/signal-dto';
import {
  emptyForm,
  formOf,
  isFormComplete,
  metricsOf,
  SignalForm,
  watchesMarket,
} from '../../models/signal-form';
import { metricLabels, targetChoices } from '../../models/signal-labels';
import { WatchedSubject, watchedSubjectOf } from '../../models/watched-subject';
import { WatchedSubjectPicker } from '../watched-subject-picker/watched-subject-picker';

const signalFrequencyOptions = [
  { label: translate('signals.editor.frequency.off'), value: 'off' },
  { label: translate('signals.editor.frequency.hourly'), value: 'hourly' },
  { label: translate('signals.editor.frequency.daily'), value: 'daily' },
  { label: translate('signals.editor.frequency.weekly'), value: 'weekly' },
];

@Component({
  selector: 'app-signal-editor',
  imports: [
    TranslocoPipe,
    FormsModule,
    Button,
    Dialog,
    InputNumber,
    InputText,
    Message,
    Select,
    ToggleSwitch,
    ReportTimingEditor,
    WatchedSubjectPicker,
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
    { label: translate('signals.editor.above'), value: 'Above' },
    { label: translate('signals.editor.below'), value: 'Below' },
  ];
  protected readonly targets = targetChoices;
  protected readonly pickerVisible = signal(false);

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
  protected readonly watchesMarket = computed(() => watchesMarket(this.form().target));
  protected readonly targetChoice = computed(() =>
    this.watchesMarket() ? 'Instrument' : this.form().target,
  );
  protected readonly watchedSubject = computed(() => watchedSubjectOf(this.form()));
  protected readonly watchedLabel = computed(() => this.labelOfWatchedSubject());

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

    this.patch({ target, metric, groupName: null });

    if (target === 'Instrument') this.pickerVisible.set(true);
  }

  protected pick(subject: WatchedSubject): void {
    const metric = this.metricFitting(subject.target);

    this.patch({
      target: subject.target,
      metric,
      instrumentId: subject.target === 'Instrument' ? subject.instrumentId : null,
      groupName: subject.target === 'InstrumentGroup' ? subject.groupName : null,
    });
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
      header: translate('signals.editor.deleteQuestion'),
      message: translate('signals.editor.deleteHint'),
      icon: 'pi pi-trash',
      acceptLabel: translate('common.delete'),
      rejectLabel: translate('common.cancel'),
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
    this.pickerVisible.set(false);
  }

  private labelOfWatchedSubject(): string | null {
    const subject = this.watchedSubject();
    if (subject?.target === 'InstrumentGroup') return subject.groupName;

    const instrumentId = subject?.instrumentId;
    const instrument = this.store.instruments().find((each) => each.id === instrumentId);

    return instrument ? `${instrument.label} · ${instrument.name}` : null;
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

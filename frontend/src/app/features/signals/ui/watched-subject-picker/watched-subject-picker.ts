import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Dialog } from 'primeng/dialog';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { SelectButton } from 'primeng/selectbutton';
import { Instrument } from '../../../../shared/models/api';
import { instrumentKindLabel } from '../../../../shared/models/instrument-kind';
import {
  instrumentGroupsOf,
  instrumentMatches,
  WatchedSubject,
} from '../../models/watched-subject';

type PickerTab = WatchedSubject['target'];

@Component({
  selector: 'app-watched-subject-picker',
  imports: [TranslocoPipe, FormsModule, Dialog, IconField, InputIcon, InputText, SelectButton],
  templateUrl: './watched-subject-picker.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WatchedSubjectPicker {
  readonly visible = model(false);
  readonly instruments = input.required<Instrument[]>();
  readonly chosen = input<WatchedSubject | null>(null);
  readonly picked = output<WatchedSubject>();

  protected readonly tabs: { label: string; value: PickerTab }[] = [
    { label: translate('signals.subject.instrument'), value: 'Instrument' },
    { label: translate('signals.subject.group'), value: 'InstrumentGroup' },
  ];
  protected readonly tab = signal<PickerTab>('Instrument');
  protected readonly search = signal('');

  protected readonly searchText = computed(() => this.search().trim().toLowerCase());
  protected readonly isInstrumentTab = computed(() => this.tab() === 'Instrument');
  protected readonly visibleInstruments = computed(() =>
    this.instruments().filter((instrument) => instrumentMatches(instrument, this.searchText())),
  );
  protected readonly visibleGroups = computed(() =>
    instrumentGroupsOf(this.instruments()).filter((group) =>
      group.name.toLowerCase().includes(this.searchText()),
    ),
  );
  protected readonly chosenInstrumentId = computed(() => {
    const chosen = this.chosen();

    return chosen?.target === 'Instrument' ? chosen.instrumentId : null;
  });
  protected readonly chosenGroupName = computed(() => {
    const chosen = this.chosen();

    return chosen?.target === 'InstrumentGroup' ? chosen.groupName : null;
  });

  constructor() {
    effect(() => {
      if (this.visible()) untracked(() => this.reset());
    });
  }

  protected kindLabel(kind: string): string {
    return instrumentKindLabel(kind);
  }

  protected changeTab(tab: PickerTab | null): void {
    if (!tab) return;

    this.tab.set(tab);
    this.search.set('');
  }

  protected pickInstrument(instrumentId: string): void {
    this.pick({ target: 'Instrument', instrumentId });
  }

  protected pickGroup(groupName: string): void {
    this.pick({ target: 'InstrumentGroup', groupName });
  }

  private pick(subject: WatchedSubject): void {
    this.picked.emit(subject);
    this.visible.set(false);
  }

  private reset(): void {
    this.tab.set(this.chosen()?.target ?? 'Instrument');
    this.search.set('');
  }
}

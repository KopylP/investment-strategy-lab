import { TranslocoPipe } from '@jsverse/transloco';
import { NgTemplateOutlet } from '@angular/common';
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
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { MultiSelect } from 'primeng/multiselect';
import { Select } from 'primeng/select';
import { FundSelectionDialog } from '../fund-selection-dialog/fund-selection-dialog';
import { DraftSetupDto } from '../../models/draft-dto';
import { InstrumentChoiceDto } from '../../models/instrument-dto';

interface InstrumentOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-builder-setup-dialog',
  imports: [
    TranslocoPipe,
    NgTemplateOutlet,
    FormsModule,
    Button,
    Dialog,
    MultiSelect,
    Select,
    FundSelectionDialog,
  ],
  templateUrl: './setup-dialog.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetupDialog {
  readonly visible = model(false);
  readonly inline = input(false);
  readonly setup = input.required<DraftSetupDto>();
  readonly instruments = input.required<InstrumentChoiceDto[]>();
  readonly saved = output<DraftSetupDto>();
  readonly instrumentsChanged = output<void>();
  protected readonly fundSearchVisible = signal(false);

  protected readonly draft = signal<DraftSetupDto>(emptySetup());
  protected readonly regularOptions = computed(() =>
    optionsOf(this.instruments().filter((item) => !item.isVolatilityIndex)),
  );
  protected readonly volatilityOptions = computed(() =>
    optionsOf(this.instruments().filter((item) => item.isVolatilityIndex)),
  );

  constructor() {
    effect(() => {
      if (this.inline()) this.draft.set(cloneSetup(this.setup()));
      else if (this.visible()) untracked(() => this.draft.set(cloneSetup(this.setup())));
    });
  }

  protected patch(change: Partial<DraftSetupDto>): void {
    this.draft.update((draft) => ({ ...draft, ...change }));
    if (this.inline()) this.saved.emit(cloneSetup(this.draft()));
  }

  protected save(): void {
    this.saved.emit(cloneSetup(this.draft()));
    this.visible.set(false);
  }
}

function optionsOf(instruments: InstrumentChoiceDto[]): InstrumentOption[] {
  return instruments.map((instrument) => ({
    label: `${instrument.label} · ${instrument.name}`,
    value: instrument.id,
  }));
}

function cloneSetup(setup: DraftSetupDto): DraftSetupDto {
  return { ...setup, funds: [...setup.funds], extraSeries: [...setup.extraSeries] };
}

function emptySetup(): DraftSetupDto {
  return {
    funds: [],
    benchmarkId: null,
    cashId: null,
    volatilityIndexId: null,
    extraSeries: [],
  };
}

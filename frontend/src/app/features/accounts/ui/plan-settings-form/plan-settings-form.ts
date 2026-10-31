import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { SelectButton } from 'primeng/selectbutton';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';

export interface PlanSettingsVm {
  monthlyContribution: number;
  minTrade: number;
  trimBandPercent: number;
  allowFractionalShares: boolean;
  generateOnSchedule: boolean;
  contributionPolicy: 'FillUnderweight' | 'FixedSplit';
  driftBandPercent: number;
}

@Component({
  selector: 'app-plan-settings-form',
  imports: [FormsModule, InputNumber, SelectButton, ToggleSwitch, Tooltip, TapTooltip],
  templateUrl: './plan-settings-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanSettingsForm {
  readonly settings = model.required<PlanSettingsVm>();
  readonly currency = input.required<string>();
  readonly showPolicy = input(true);

  protected readonly policies = [
    { label: 'Тому, хто відстає', value: 'FillUnderweight' },
    { label: 'За частками', value: 'FixedSplit' },
  ];

  protected set<K extends keyof PlanSettingsVm>(key: K, value: PlanSettingsVm[K]): void {
    this.settings.update((current) => ({ ...current, [key]: value }));
  }
}

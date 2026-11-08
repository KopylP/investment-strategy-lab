import { translate } from '@jsverse/transloco';
import { InstrumentPickerVm } from '../models/instrument-picker-vm';

const fundLimit = 60;

export const fundPicker: InstrumentPickerVm = {
  title: translate('builder.pickers.funds.title'),
  intro: translate('builder.pickers.funds.hint'),
  chooseLabel: translate('builder.setup.chooseFunds'),
  changeLabel: translate('builder.pickers.funds.change'),
  limit: fundLimit,
  volatilityIndices: false,
  externalSearch: true,
  required: true,
};

export const setupPickers: Record<string, InstrumentPickerVm> = {
  funds: fundPicker,
  benchmarkId: {
    title: translate('strategies.detail.benchmark'),
    intro: translate('builder.pickers.benchmark.hint'),
    chooseLabel: translate('builder.pickers.benchmark.choose'),
    changeLabel: translate('builder.pickers.benchmark.change'),
    limit: 1,
    volatilityIndices: false,
    externalSearch: true,
    required: true,
  },
  volatilityIndexId: {
    title: translate('strategies.detail.volatilityIndex'),
    intro: translate('builder.pickers.vix.hint'),
    chooseLabel: translate('builder.pickers.vix.choose'),
    changeLabel: translate('builder.pickers.vix.change'),
    limit: 1,
    volatilityIndices: true,
    externalSearch: false,
    required: false,
  },
  extraSeries: {
    title: translate('builder.setup.extraSeries'),
    intro: translate('builder.pickers.extra.hint'),
    chooseLabel: translate('builder.pickers.extra.choose'),
    changeLabel: translate('builder.pickers.extra.change'),
    limit: fundLimit,
    volatilityIndices: false,
    externalSearch: true,
    required: false,
  },
};

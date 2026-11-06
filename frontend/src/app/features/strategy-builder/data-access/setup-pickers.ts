import { InstrumentPickerVm } from '../models/instrument-picker-vm';

const fundLimit = 60;

export const fundPicker: InstrumentPickerVm = {
  title: 'Фонди стратегії',
  intro:
    'Виберіть фонди, серед яких стратегія шукатиме кандидатів. Зміни застосуються після підтвердження.',
  chooseLabel: 'Обрати фонди',
  changeLabel: 'Змінити фонди',
  limit: fundLimit,
  volatilityIndices: false,
  externalSearch: true,
  required: true,
};

export const setupPickers: Record<string, InstrumentPickerVm> = {
  funds: fundPicker,
  benchmarkId: {
    title: 'Бенчмарк',
    intro:
      'Виберіть фонд, з яким стратегія порівнюватиметься і за яким може визначати ринковий режим.',
    chooseLabel: 'Обрати бенчмарк',
    changeLabel: 'Змінити бенчмарк',
    limit: 1,
    volatilityIndices: false,
    externalSearch: true,
    required: true,
  },
  volatilityIndexId: {
    title: 'Індекс волатильності',
    intro: 'Виберіть індекс, на який можуть спиратися ринкові умови стратегії.',
    chooseLabel: 'Обрати індекс',
    changeLabel: 'Змінити індекс',
    limit: 1,
    volatilityIndices: true,
    externalSearch: false,
    required: false,
  },
  extraSeries: {
    title: 'Додаткові ряди',
    intro: 'Виберіть інструменти, ціни яких стратегія може використовувати в індикаторах.',
    chooseLabel: 'Обрати ряди',
    changeLabel: 'Змінити ряди',
    limit: fundLimit,
    volatilityIndices: false,
    externalSearch: true,
    required: false,
  },
};

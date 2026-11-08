import { translate } from '@jsverse/transloco';
import { JsonValue } from '../models/draft-dto';
import { ExperimentParameterVm } from '../models/experiment-parameter-vm';
import { VocabularyDto, VocabularyFieldDto } from '../models/vocabulary-dto';

const sections: Record<string, string> = {
  selection: translate('builder.parameters.group.selection'),
  weights: translate('builder.parameters.group.weights'),
  risk: translate('builder.parameters.group.risk'),
  marketFilter: translate('builder.parameters.group.market'),
};
const seriesNames: Record<string, string> = {
  fund: translate('builder.graph.setup.fund'), benchmark: translate('strategies.detail.benchmark'), volatilityIndex: translate('strategies.detail.volatilityIndex'), extra: translate('builder.graph.setup.extraSeries'),
};

export function experimentParameters(rules: Record<string, JsonValue>, vocabulary: VocabularyDto): ExperimentParameterVm[] {
  const choices: ExperimentParameterVm[] = [];
  const indicators = objectOf(rules['indicators']);

  for (const [name, value] of Object.entries(indicators)) {
    const indicator = objectOf(value);
    const kind = vocabulary.indicators.find((item) => item.code === indicator['kind']);
    if (!kind) continue;
    const series = objectOf(indicator['of']);
    const source = seriesNames[String(series['series'])] ?? '';
    const group = `${kind.label} · ${source}`;
    choices.push(...numericChoices(indicator, `indicators.${name}`, group, kind.fields));
  }

  for (const [section, label] of Object.entries(sections)) {
    const definition = objectOf(rules[section]);
    choices.push(...numericChoices(definition, section, label, Object.values(vocabulary.blockFields)));
  }

  return choices;
}

function numericChoices(definition: Record<string, JsonValue>, path: string, group: string, fields: VocabularyFieldDto[]): ExperimentParameterVm[] {
  const choices: ExperimentParameterVm[] = [];

  for (const field of fields) {
    const value = definition[field.name];
    if (typeof value !== 'number') continue;
    choices.push({ path: `${path}.${field.name}`, label: `${group} · ${field.label}`, currentValue: value, percent: field.kind === 'Fraction' });
  }

  return choices;
}

function objectOf(value: JsonValue | undefined): Record<string, JsonValue> {
  return value !== null && typeof value === 'object' && !Array.isArray(value) ? value : {};
}

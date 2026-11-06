import { ExperimentConditionsDto } from '../models/experiment-conditions-dto';
import { ExperimentFormVm } from '../models/experiment-form-vm';
import { ExperimentParameterVm } from '../models/experiment-parameter-vm';

export function defaultExperimentForm(today = new Date()): ExperimentFormVm {
  const year = today.getFullYear();

  return {
    label: '',
    start: `${year - 5}-01-01`,
    end: localDate(today),
    initialCapital: 100_000,
    commissionPercent: 0.1,
    feePerTrade: 1,
    spreadBps: 0,
    slippageBps: 0,
    independent: false,
    outOfSampleStart: `${year - 2}-01-01`,
    walkForward: false,
    trainingMonths: 36,
    testMonths: 12,
    sensitivity: false,
    parameterPath: null,
    parameterValues: '',
  };
}

export function experimentFromForm(form: ExperimentFormVm, parameter: ExperimentParameterVm | null): ExperimentConditionsDto {
  const values = form.parameterValues.split(';').map((value) => Number(value.trim().replace(',', '.')));
  const scale = parameter?.percent ? 100 : 1;

  return {
    period: { start: form.start, end: form.end },
    initialCapital: form.initialCapital,
    costs: {
      commissionRate: form.commissionPercent / 100,
      feePerTrade: form.feePerTrade,
      spreadBps: form.spreadBps,
      slippageBps: form.slippageBps,
    },
    robustness: {
      outOfSampleStart: form.independent ? form.outOfSampleStart : null,
      walkForward: form.walkForward ? { trainingMonths: form.trainingMonths, testMonths: form.testMonths } : null,
      neighborhood: form.sensitivity && parameter ? { path: parameter.path, values: values.map((value) => value / scale) } : null,
    },
  };
}

export function valuesAreNumbers(text: string): boolean {
  return text.trim().length > 0 && text.split(';').every((value) => value.trim().length > 0 && Number.isFinite(Number(value.trim().replace(',', '.'))));
}

function localDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${date.getFullYear()}-${month}-${day}`;
}

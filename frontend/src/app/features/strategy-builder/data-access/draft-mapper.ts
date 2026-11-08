import { translate } from '@jsverse/transloco';
import { DraftDto } from '../models/draft-dto';
import { SetupSummaryVm } from '../models/draft-vm';
import { InstrumentChoiceDto } from '../models/instrument-dto';

const shownFunds = 6;

export function setupSummary(
  draft: DraftDto,
  instruments: InstrumentChoiceDto[],
  reportNames: Record<string, string>,
): SetupSummaryVm {
  const labels = new Map(instruments.map((instrument) => [instrument.id, instrument.label]));
  const labelOf = (id: string | null) =>
    id ? (labels.get(id) ?? '—') : translate('builder.graph.node.notChosen');
  const funds = draft.setup.funds.map((id) => labels.get(id) ?? '—');

  return {
    fundCount: funds.length,
    fundLabels: funds.slice(0, shownFunds),
    moreFunds: Math.max(0, funds.length - shownFunds),
    benchmark: labelOf(draft.setup.benchmarkId),
    cash: labelOf(draft.setup.cashId),
    reports:
      draft.reports.map((report) => reportNames[report.code] ?? report.code).join(', ') ||
      translate('builder.mapper.none'),
  };
}

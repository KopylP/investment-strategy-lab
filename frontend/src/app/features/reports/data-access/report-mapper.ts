import { translate } from '@jsverse/transloco';
import { formatMoney, formatPeriod, formatQuantity } from '../../../shared/pipes/format';
import {
  ActionKind,
  ReportActionDto,
  ReportDetailsDto,
  ReportRequestDto,
  ReportSectionDto,
  ReportSummaryDto,
  SectionColumnDto,
  SectionOrderDto,
  SectionScoreDto,
  TaxEstimateDto,
} from '../models/report-dto';
import {
  ReportActionVm,
  ReportProgressVm,
  ReportRequestVm,
  ReportSummaryVm,
  ReportVm,
  ScoreTableVm,
  SectionVm,
  TaxImpactVm,
} from '../models/report-vm';

const kindLabels: Record<ReportSummaryDto['kind'], string> = {
  Monthly: translate('reports.mapper.kind.Monthly'),
  Recalculation: translate('reports.mapper.kind.Recalculation'),
  RiskReview: translate('reports.mapper.kind.RiskReview'),
};

const triggerLabels: Record<ReportSummaryDto['trigger'], string> = {
  Scheduled: translate('reports.mapper.trigger.Scheduled'),
  OnDemand: translate('reports.mapper.trigger.OnDemand'),
  Signal: translate('reports.mapper.trigger.Signal'),
};

export function toRequestVm(request: ReportRequestDto): ReportRequestVm {
  return {
    id: request.id,
    code: request.code,
    title: `${request.title} · ${formatPeriod(request.asOf.slice(0, 7))}`,
    kindLabel: kindLabels[request.kind],
    triggerLabel: triggerLabels[request.trigger],
    status: request.status === 'Failed' ? 'Failed' : 'Pending',
    requestedAt: request.requestedAt,
    progressLabel:
      request.strategies > 0
        ? translate('reports.list.ofTotal', {
            open: request.strategiesAnswered,
            total: request.strategies,
          })
        : null,
    error: request.error,
  };
}

export function toSummaryVm(summary: ReportSummaryDto): ReportSummaryVm {
  const periodLabel = formatPeriod(summary.period);

  return {
    id: summary.id,
    code: summary.code,
    accountId: summary.accountId,
    title: `${summary.title} · ${periodLabel}`,
    periodLabel,
    kind: summary.kind,
    kindLabel: kindLabels[summary.kind],
    status: summary.status,
    triggerLabel: triggerLabels[summary.trigger],
    marketAsOf: summary.marketAsOf,
    generatedAt: summary.generatedAt,
    closedAt: summary.closedAt,
    currency: summary.currency,
    contribution: summary.contribution,
    actionsTotal: summary.actionsTotal,
    actionsOpen: summary.actionsOpen,
    supersededBy: summary.supersededBy,
    accountName: summary.accountName,
  };
}

const guid = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

function plannedAmount(actions: { plannedAmount: number }[]): number {
  return actions.reduce((sum, action) => sum + action.plannedAmount, 0);
}

function toContributionVm(
  report: ReportDetailsDto,
  subName: (id: string | null) => string,
): ReportVm['contribution'] {
  const plan = report.contributionPlan;
  const splitLabel =
    plan.split === 'FixedSplit'
      ? translate('reports.mapper.split.FixedSplit')
      : translate('reports.mapper.split.laggard');
  const parts = plan.parts.map((part) => ({
    name: subName(part.subPortfolioId),
    amount: part.amount,
  }));

  return { total: plan.total, splitLabel, parts };
}

export function toReportVm(report: ReportDetailsDto): ReportVm {
  const summary = toSummaryVm(report.summary);
  const symbol = (id: string | null) =>
    id ? (report.instruments[id]?.symbol ?? id.slice(0, 8)) : '';
  const readable = (text: string) =>
    text.replace(guid, (id) => report.instruments[id.toLowerCase()]?.symbol ?? id);
  const subName = (id: string | null) =>
    id
      ? (report.subPortfolios[id] ?? translate<string>('reports.mapper.subPortfolioFallback'))
      : '';
  const isOpenForMarking = summary.status === 'New' || summary.status === 'InProgress';

  const actions = report.actions.map((action) =>
    toActionVm(action, summary.currency, symbol, readable, subName, isOpenForMarking, report),
  );

  return {
    summary,
    isOpenForMarking,
    actions,
    sections: report.sections.map((section) => toSectionVm(section, symbol, readable)),
    warnings: [...new Set(report.warnings)],
    quotesLookStale: report.warnings.some((warning) =>
      warning.includes(translate('reports.mapper.quoteWarningMarker')),
    ),
    contribution: toContributionVm(report, subName),
    review: report.review,
    taxImpact: toTaxImpactVm(report.taxEstimate, actions),
    totals: plannedTotals(actions),
  };
}

function signedTax(change: number, currency: string): string {
  if (change === 0) return translate('reports.mapper.noChange');

  const sign = change > 0 ? '+' : '−';

  return `${sign}${formatMoney(Math.abs(change), currency, 0)}`;
}

function toTaxImpactVm(
  estimate: TaxEstimateDto | null,
  actions: ReportActionVm[],
): TaxImpactVm | null {
  if (!estimate?.isTaxed || !estimate.taxCurrency) return null;

  const currency = estimate.taxCurrency;
  const notes = estimate.notes.filter((note) => note.length > 0);

  if (estimate.yearTaxBefore === null || estimate.yearTaxBefore === undefined) {
    return {
      change: translate('reports.mapper.taxApprox', {
        amount: formatMoney(estimate.taxChange, currency, 0),
      }),
      yearChange: null,
      carriedLoss: null,
      rowsNote: null,
      notes,
    };
  }

  const before = estimate.yearTaxBefore;
  const after = before + estimate.taxChange;
  const salesCount = actions.filter((action) => action.taxLabel !== null).length;

  return {
    change: translate('reports.mapper.taxApprox', {
      amount: signedTax(estimate.taxChange, currency),
    }),
    yearChange: translate('reports.mapper.yearChange', {
      year: estimate.year,
      before: formatMoney(before, currency, 0),
      after: formatMoney(after, currency, 0),
    }),
    carriedLoss: carriedLossLabel(estimate.carriedLoss, currency),
    rowsNote: salesCount > 1 ? translate('reports.mapper.salesHint') : null,
    notes,
  };
}

function carriedLossLabel(carriedLoss: number | null, currency: string): string | null {
  return carriedLoss
    ? translate('reports.mapper.carriedLoss', { amount: formatMoney(carriedLoss, currency, 0) })
    : null;
}

function saleTaxLabel(action: ReportActionDto, estimate: TaxEstimateDto | null): string | null {
  if (action.estimatedTaxChange === null || !estimate?.isTaxed || !estimate.taxCurrency)
    return null;

  const currency = estimate.taxCurrency;
  const gain = action.estimatedGain ?? 0;

  if (estimate.yearTaxBefore === null || estimate.yearTaxBefore === undefined) {
    return translate('reports.mapper.taxWithGain', {
      tax: formatMoney(action.estimatedTaxChange, currency, 0),
      gain: formatMoney(gain, currency, 0),
    });
  }

  const outcome =
    gain < 0
      ? translate('reports.mapper.loss', { amount: formatMoney(-gain, currency, 0) })
      : translate('reports.mapper.gain', { amount: formatMoney(gain, currency, 0) });
  const carriedLoss = action.estimatedCarriedLoss;
  const details = carriedLoss
    ? translate('reports.mapper.outcomeWithCarried', {
        outcome,
        amount: formatMoney(carriedLoss, currency, 0),
      })
    : outcome;

  return translate('reports.mapper.taxWithDetails', {
    tax: signedTax(action.estimatedTaxChange, currency),
    details,
  });
}

function plannedTotals(actions: ReportActionVm[]) {
  const buys = actions.filter((action) => action.kind === 'Buy' || action.kind === 'Park');
  const sells = actions.filter((action) => action.isSell);

  return { buys: plannedAmount(buys), sells: plannedAmount(sells) };
}

function toActionVm(
  action: ReportActionDto,
  currency: string,
  symbol: (id: string | null) => string,
  readable: (text: string) => string,
  subName: (id: string | null) => string,
  isOpen: boolean,
  report: ReportDetailsDto,
): ReportActionVm {
  const ticker = symbol(action.instrumentId);
  const isMoney = action.kind === 'Contribute';
  const fillSummary =
    action.status === 'PartiallyExecuted'
      ? translate(isMoney ? 'reports.mapper.fillMoney' : 'reports.mapper.fillUnits', {
          filled: formatQuantity(action.filledQuantity),
          planned: formatQuantity(action.plannedQuantity),
        })
      : undefined;
  const canMark = isOpen && (action.status === 'Pending' || action.status === 'PartiallyExecuted');

  return {
    id: action.id,
    kind: action.kind,
    title: titleOf(
      action.kind,
      ticker,
      subName(action.fromSubPortfolioId),
      subName(action.toSubPortfolioId),
    ),
    instrumentName: action.instrumentId
      ? (report.instruments[action.instrumentId]?.name ?? '')
      : '',
    instrumentId: action.instrumentId,
    symbol: ticker,
    reason: readable(action.reason),
    isMoney,
    plannedQuantity: action.plannedQuantity,
    plannedPrice: action.plannedPrice,
    plannedAmount: action.plannedAmount,
    currency,
    status: action.status,
    filledQuantity: action.filledQuantity,
    remainingQuantity: action.remainingQuantity,
    fillSummary,
    fills: action.fills,
    awaitingJournal: action.fills.some((fill) => !fill.transactionId),
    skipReason: action.skipReason,
    rejection: action.rejection,
    canMark,
    canPartiallyFill: canMark && !isMoney && action.kind !== 'Transfer',
    canSkip: isOpen && action.status === 'Pending',
    canUndo: action.status !== 'Pending' && report.summary.status !== 'Superseded',
    allocations: action.allocations.map((allocation) => ({
      subPortfolio: subName(allocation.subPortfolioId),
      quantity: allocation.quantity,
      reason: allocation.reason && readable(allocation.reason),
    })),
    taxLabel: saleTaxLabel(action, report.taxEstimate),
    hasBreakdown: action.allocations.length > 1 || action.fills.length > 0,
    isSell: action.kind === 'Sell' || action.kind === 'Unpark',
  };
}

function titleOf(kind: ActionKind, ticker: string, from: string, to: string): string {
  switch (kind) {
    case 'Contribute':
      return translate('reports.mapper.contribute');
    case 'Buy':
    case 'Sell':
    case 'Park':
    case 'Unpark':
      return translate(`reports.mapper.title.${kind}`, { ticker });
    case 'Transfer':
      return translate('reports.mapper.title.Transfer', { ticker, from, to });
  }
}

const orderLabels: Record<
  SectionOrderDto['kind'],
  { label: string; severity: SectionVm['orders'][number]['severity'] }
> = {
  Exit: { label: translate('reports.mapper.actionKind.Exit'), severity: 'danger' },
  Trim: { label: translate('reports.mapper.actionKind.Trim'), severity: 'warn' },
  Unpark: { label: translate('reports.mapper.actionKind.Unpark'), severity: 'info' },
  Buy: { label: translate('reports.mapper.actionKind.Buy'), severity: 'success' },
  Park: { label: translate('reports.mapper.actionKind.Park'), severity: 'secondary' },
};

const legacyColumns: SectionColumnDto[] = [
  {
    id: 'momentum',
    label: translate('reports.mapper.column.momentum'),
    format: 'signedPercent',
    hint: null,
    compact: true,
    trueHint: null,
    falseHint: null,
  },
  {
    id: 'return1m',
    label: translate('reports.mapper.column.return1m'),
    format: 'signedPercent',
    hint: null,
    compact: false,
    trueHint: null,
    falseHint: null,
  },
  {
    id: 'return3m',
    label: translate('reports.mapper.column.return3m'),
    format: 'signedPercent',
    hint: null,
    compact: false,
    trueHint: null,
    falseHint: null,
  },
  {
    id: 'return12m',
    label: translate('reports.mapper.column.return12m'),
    format: 'signedPercent',
    hint: null,
    compact: false,
    trueHint: null,
    falseHint: null,
  },
  {
    id: 'volatility',
    label: translate('reports.mapper.column.volatility'),
    format: 'percent',
    hint: null,
    compact: false,
    trueHint: null,
    falseHint: null,
  },
  {
    id: 'ma200',
    label: 'MA200',
    format: 'trend',
    hint: null,
    compact: true,
    trueHint: translate('reports.mapper.column.aboveMa'),
    falseHint: translate('reports.mapper.column.belowMa'),
  },
];

function legacyValues(score: SectionScoreDto): Record<string, number | boolean | null> {
  return {
    momentum: score.momentum,
    return1m: score.return1m,
    return3m: score.return3m,
    return12m: score.return12m,
    volatility: score.volatility,
    ma200: score.aboveMovingAverage,
  };
}

function toScoreTable(
  section: ReportSectionDto,
  targets: Map<string, number>,
  symbol: (id: string | null) => string,
  readable: (text: string) => string,
): ScoreTableVm | null {
  const decision = section.decision;
  if (!decision?.scores.length) return null;

  const described = decision.columns != null;
  const columns = decision.columns ?? legacyColumns;

  return {
    title:
      decision.scoreTitle ??
      (described
        ? translate('reports.mapper.whyInstruments')
        : translate('reports.mapper.whyEtfs')),
    instrumentLabel:
      decision.instrumentLabel ?? (described ? translate('instruments.instrument') : 'ETF'),
    columns,
    rows: decision.scores
      .map((score) => {
        const values = described ? (score.values ?? {}) : legacyValues(score);

        return {
          symbol: symbol(score.instrumentId),
          name: score.name,
          group: score.group,
          rank: score.rank,
          values: columns.map((column) => values[column.id] ?? null),
          status: score.status,
          reason: readable(score.reason),
          isTarget: targets.has(score.instrumentId),
          targetWeight: targets.get(score.instrumentId) ?? null,
        };
      })
      .sort((left, right) => (left.rank ?? 999) - (right.rank ?? 999)),
  };
}

function toSectionVm(
  section: ReportSectionDto,
  symbol: (id: string | null) => string,
  readable: (text: string) => string,
): SectionVm {
  const targets = new Map(
    section.decision?.targets.map((target) => [target.instrumentId, target.weight]) ?? [],
  );

  return {
    id: section.subPortfolioId,
    name: section.name,
    targetShare: section.targetShare,
    strategyLabel: section.strategyCode
      ? translate('reports.mapper.strategyVersion', {
          code: section.strategyCode,
          version: section.strategyVersion,
        })
      : translate('reports.mapper.strategyNoAnswer'),
    contribution: section.contribution,
    marketIsFavorable: section.decision?.marketIsFavorable ?? null,
    investFraction: section.decision?.investFraction ?? null,
    marketReasons: section.decision?.marketReasons ?? [],
    summary: readable(section.decision?.summary ?? ''),
    warnings: section.decision?.warnings ?? [],
    notes: section.notes.map(readable),
    orders: section.orders.map((order) => ({
      kindLabel: orderLabels[order.kind].label,
      severity: orderLabels[order.kind].severity,
      symbol: symbol(order.instrumentId),
      quantity: order.quantity,
      price: order.price,
      amount: order.amount,
      reason: readable(order.reason),
    })),
    allocation: section.allocation.map((row) => ({
      label:
        row.role === 'Cash'
          ? row.instrumentId
            ? translate('reports.mapper.cashIn', { symbol: symbol(row.instrumentId) })
            : translate('common.cash')
          : symbol(row.instrumentId),
      role: row.role,
      valueBefore: row.valueBefore,
      valueAfter: row.valueAfter,
      weightBefore: row.weightBefore,
      weightAfter: row.weightAfter,
      targetWeight: row.targetWeight,
    })),
    scores: toScoreTable(section, targets, symbol, readable),
    totalBefore: section.totalBefore,
    totalAfter: section.totalAfter,
    parkedBefore: section.parkedBefore,
    parkedAfter: section.parkedAfter,
    risk: section.risk
      ? {
          ...section.risk,
          marketAlerts: section.risk.marketAlerts.map(readable),
          portfolioAlerts: section.risk.portfolioAlerts.map(readable),
          positionsVm: section.risk.positions.map((position) => ({
            symbol: symbol(position.instrumentId),
            status: position.status,
            quantity: position.quantity,
            value: position.value,
            pnlPct: position.pnlPct,
            drawdownFromHigh: position.drawdownFromHigh,
            exitPrice: position.exitPrice,
            exitRule: position.exitRule,
            exitDistance: position.exitDistance,
            alerts: position.alerts.map(readable),
            sell: position.sell,
          })),
        }
      : null,
    error: section.error,
  };
}

export function summarizeProgress(actions: readonly ReportActionVm[]): ReportProgressVm {
  const executed = actions.filter((action) => action.status === 'Executed').length;
  const skipped = actions.filter((action) => action.status === 'Skipped').length;
  const partial = actions.filter((action) => action.status === 'PartiallyExecuted').length;
  const resolved = executed + skipped;

  return {
    total: actions.length,
    resolved,
    executed,
    partial,
    skipped,
    percent: actions.length ? Math.round((resolved / actions.length) * 100) : 100,
  };
}

import { formatMoney, formatPeriod, formatQuantity } from '../../../shared/pipes/format';
import { ActionKind, ReportActionDto, ReportDetailsDto, ReportSectionDto, ReportSummaryDto, SectionOrderDto } from '../models/report-dto';
import { ReportActionVm, ReportProgressVm, ReportSummaryVm, ReportVm, SectionVm } from '../models/report-vm';

const kindLabels: Record<ReportSummaryDto['kind'], string> = {
  Monthly: 'Щомісячний',
  Recalculation: 'Перерахунок',
  RiskReview: 'Ризик-рев’ю',
};

export function toSummaryVm(summary: ReportSummaryDto): ReportSummaryVm {
  const periodLabel = formatPeriod(summary.period);
  return {
    id: summary.id,
    accountId: summary.accountId,
    title: summary.kind === 'RiskReview' ? `Ризик-рев’ю · ${periodLabel}` : `Рапорт за ${periodLabel}`,
    periodLabel,
    kind: summary.kind,
    kindLabel: kindLabels[summary.kind],
    status: summary.status,
    triggerLabel: summary.trigger === 'Scheduled' ? 'за розкладом' : 'на вимогу',
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

export function toReportVm(report: ReportDetailsDto): ReportVm {
  const summary = toSummaryVm(report.summary);
  const symbol = (id: string | null) => (id ? (report.instruments[id]?.symbol ?? id.slice(0, 8)) : '');
  // The engine words its reasons with instrument ids; people read tickers.
  const readable = (text: string) => text.replace(guid, (id) => report.instruments[id.toLowerCase()]?.symbol ?? id);
  const subName = (id: string | null) => (id ? (report.subPortfolios[id] ?? 'під-портфель') : '');
  const isOpenForMarking = summary.status === 'New' || summary.status === 'InProgress';

  const actions = report.actions.map((action) => toActionVm(action, summary.currency, symbol, subName, isOpenForMarking, report));
  return {
    summary,
    isOpenForMarking,
    actions,
    sections: report.sections.map((section) => toSectionVm(section, symbol, readable)),
    warnings: report.warnings,
    quotesLookStale: report.warnings.some((warning) => warning.includes('котирув')),
    contribution: {
      total: report.contributionPlan.total,
      splitLabel: report.contributionPlan.split === 'FixedSplit' ? 'за цільовими частками' : 'тому, хто найбільше відстає',
      parts: report.contributionPlan.parts.map((part) => ({ name: subName(part.subPortfolioId), amount: part.amount })),
      transfer: report.contributionPlan.transfer
        ? {
            from: subName(report.contributionPlan.transfer.fromSubPortfolioId),
            to: subName(report.contributionPlan.transfer.toSubPortfolioId),
            amount: report.contributionPlan.transfer.amount,
          }
        : null,
    },
    review: report.review,
    taxEstimate: report.taxEstimate,
    totals: {
      buys: actions.filter((action) => action.kind === 'Buy' || action.kind === 'Park').reduce((sum, action) => sum + action.plannedAmount, 0),
      sells: actions.filter((action) => action.isSell).reduce((sum, action) => sum + action.plannedAmount, 0),
    },
  };
}

function toActionVm(
  action: ReportActionDto,
  currency: string,
  symbol: (id: string | null) => string,
  subName: (id: string | null) => string,
  isOpen: boolean,
  report: ReportDetailsDto,
): ReportActionVm {
  const ticker = symbol(action.instrumentId);
  const isMoney = action.kind === 'Contribute';
  const unit = isMoney ? '' : ' шт.';
  const fillSummary =
    action.status === 'PartiallyExecuted'
      ? `${formatQuantity(action.filledQuantity)} з ${formatQuantity(action.plannedQuantity)}${unit}`
      : undefined;
  const canMark = isOpen && (action.status === 'Pending' || action.status === 'PartiallyExecuted');

  return {
    id: action.id,
    kind: action.kind,
    title: titleOf(action.kind, ticker, subName(action.fromSubPortfolioId), subName(action.toSubPortfolioId)),
    instrumentName: action.instrumentId ? (report.instruments[action.instrumentId]?.name ?? '') : '',
    symbol: ticker,
    reason: action.reason,
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
      reason: allocation.reason,
    })),
    taxLabel:
      action.estimatedTaxPln !== null && report.taxEstimate?.isTaxed
        ? `≈ податок ${formatMoney(action.estimatedTaxPln, 'PLN', 0)} (зиск ${formatMoney(action.estimatedGainPln, 'PLN', 0)})`
        : null,
    hasBreakdown: action.allocations.length > 1 || action.fills.length > 0,
    isSell: action.kind === 'Sell' || action.kind === 'Unpark',
  };
}

function titleOf(kind: ActionKind, ticker: string, from: string, to: string): string {
  switch (kind) {
    case 'Contribute':
      return 'Внести гроші на рахунок';
    case 'Buy':
      return `Купити ${ticker}`;
    case 'Sell':
      return `Продати ${ticker}`;
    case 'Park':
      return `Запаркувати в ${ticker}`;
    case 'Unpark':
      return `Продати ${ticker} (розпаркувати)`;
    case 'Transfer':
      return `Переказати ${ticker}: ${from} → ${to}`;
  }
}

const orderLabels: Record<SectionOrderDto['kind'], { label: string; severity: SectionVm['orders'][number]['severity'] }> = {
  Exit: { label: 'Вихід', severity: 'danger' },
  Trim: { label: 'Зменшити', severity: 'warn' },
  Unpark: { label: 'Розпаркувати', severity: 'info' },
  Buy: { label: 'Купити', severity: 'success' },
  Park: { label: 'Запаркувати', severity: 'secondary' },
};

function toSectionVm(section: ReportSectionDto, symbol: (id: string | null) => string, readable: (text: string) => string): SectionVm {
  const targets = new Map(section.decision?.targets.map((target) => [target.instrumentId, target.weight]) ?? []);
  return {
    id: section.subPortfolioId,
    name: section.name,
    targetShare: section.targetShare,
    strategyLabel: section.strategyCode ? `${section.strategyCode} · версія ${section.strategyVersion}` : 'стратегія не відповіла',
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
      reason: order.reason,
    })),
    allocation: section.allocation.map((row) => ({
      label: row.role === 'Cash' ? (row.instrumentId ? `Кеш · ${symbol(row.instrumentId)}` : 'Кеш') : symbol(row.instrumentId),
      role: row.role,
      valueBefore: row.valueBefore,
      valueAfter: row.valueAfter,
      weightBefore: row.weightBefore,
      weightAfter: row.weightAfter,
      targetWeight: row.targetWeight,
    })),
    scores: (section.decision?.scores ?? [])
      .map((score) => ({
        symbol: symbol(score.instrumentId),
        name: score.name,
        group: score.group,
        rank: score.rank,
        momentum: score.momentum,
        return1m: score.return1m,
        return3m: score.return3m,
        return12m: score.return12m,
        volatility: score.volatility,
        aboveMovingAverage: score.aboveMovingAverage,
        status: score.status,
        reason: readable(score.reason),
        isTarget: targets.has(score.instrumentId),
        targetWeight: targets.get(score.instrumentId) ?? null,
      }))
      .sort((left, right) => (left.rank ?? 999) - (right.rank ?? 999)),
    totalBefore: section.totalBefore,
    totalAfter: section.totalAfter,
    parkedBefore: section.parkedBefore,
    parkedAfter: section.parkedAfter,
    risk: section.risk
      ? {
          ...section.risk,
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
            alerts: position.alerts,
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
  return { total: actions.length, resolved, executed, partial, skipped, percent: actions.length ? Math.round((resolved / actions.length) * 100) : 100 };
}

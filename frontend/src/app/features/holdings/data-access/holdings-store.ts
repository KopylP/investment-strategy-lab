import { translate } from '@jsverse/transloco';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DisplayCurrency } from '../../../core/context/display-currency';
import { ChartTheme } from '../../../shared/charts/chart-theme';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { toIsoDate } from '../../../shared/models/iso-date';
import { exposureBreakdownsOf } from '../models/exposure-vm';
import { SleeveVm } from '../models/holdings-vm';
import { ValuedHoldingsDto } from '../models/valued-holdings-dto';
import { HoldingsMapper, SleeveSetup } from './holdings-mapper';

const MIN_VISIBLE_COMPOSITION_WEIGHT = 0.0005;

@Injectable()
export class HoldingsStore {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);
  private readonly instruments = inject(InstrumentCatalog);
  private readonly fx = inject(DisplayCurrency);
  private readonly theme = inject(ChartTheme);
  private readonly mapper = inject(HoldingsMapper);

  readonly asOf = signal<Date | null>(null);

  private readonly holdingsResource = resource({
    params: () => {
      const accountIds = this.accountIdsInView();

      return accountIds.length
        ? { accountIds, asOf: this.asOf() ? toIsoDate(this.asOf()!) : undefined }
        : undefined;
    },
    loader: ({ params }) =>
      Promise.all(
        params.accountIds.map((id) =>
          this.api.get<ValuedHoldingsDto>(`/api/portfolio/accounts/${id}/holdings/valued`, {
            asOf: params.asOf,
          }),
        ),
      ),
  });

  readonly account = this.accounts.current;
  readonly isAll = this.accounts.isAll;
  readonly isLoading = this.holdingsResource.isLoading;
  readonly loadError = this.holdingsResource.error;
  readonly currency = computed(() =>
    this.isAll()
      ? (this.fx.code() ?? this.account()?.baseCurrency ?? 'EUR')
      : (this.holdingsResource.value()?.[0]?.currency ?? this.account()?.baseCurrency ?? 'EUR'),
  );

  private readonly holdingsInView = computed(() =>
    (this.holdingsResource.value() ?? []).map((holdings) => ({
      holdings,
      rate: this.rateToViewCurrency(holdings.currency),
    })),
  );

  readonly sleeves = computed<SleeveVm[]>(() =>
    this.holdingsInView().flatMap(({ holdings, rate }) =>
      this.mapper.sleevesOf(holdings, this.setupOf(holdings, rate)),
    ),
  );

  readonly totals = computed(() => {
    const accounts = this.holdingsInView().map(({ holdings, rate }) =>
      this.mapper.totalsOf(holdings, rate),
    );
    const value = accounts.reduce((sum, account) => sum + account.value, 0);
    const cash = accounts.reduce((sum, account) => sum + account.cash, 0);
    const costBasis = accounts.reduce((sum, account) => sum + account.costBasis, 0);
    const pnl = accounts.reduce((sum, account) => sum + account.pnl, 0);
    const cashParts = this.cashPartsInView();

    return {
      value,
      cash,
      cashParts,
      hasForeignCash: cashParts.length > 1,
      invested: value - cash,
      costBasis,
      pnl,
      pnlPct: costBasis ? pnl / costBasis : null,
    };
  });

  readonly positionCount = computed(
    () =>
      new Set(
        this.sleeves().flatMap((sleeve) =>
          sleeve.positions.map((position) => position.instrumentId),
        ),
      ).size,
  );

  readonly composition = computed(() => {
    const total = this.totals().value;
    if (!total) return [];

    const byInstrument = new Map<string, { name: string; value: number }>();

    for (const position of this.sleeves().flatMap((sleeve) => sleeve.positions))
      byInstrument.set(position.symbol, {
        name: position.name,
        value: (byInstrument.get(position.symbol)?.value ?? 0) + position.value,
      });

    const palette = this.theme.palette();
    const colors = [...palette.series, palette.gain, palette.tax];
    const slices = [...byInstrument]
      .sort((left, right) => right[1].value - left[1].value)
      .map(([label, { name, value }], index) => ({
        label,
        name,
        weight: value / total,
        color: colors[index % colors.length],
      }));
    const cash = this.totals().cash;
    const withCash =
      cash > 0
        ? [
            ...slices,
            {
              label: translate('common.cash'),
              name: '',
              weight: cash / total,
              color: palette.neutral,
            },
          ]
        : slices;

    return withCash.filter((slice) => slice.weight >= MIN_VISIBLE_COMPOSITION_WEIGHT);
  });

  readonly exposure = computed(() => {
    const catalog = this.instruments.byId();
    const currency = this.currency();
    const holdings = this.sleeves()
      .flatMap((sleeve) => sleeve.positions)
      .map((position) => {
        const instrument = catalog.get(position.instrumentId);

        return {
          value: position.value,
          assetClass: instrument?.assetClass ?? null,
          region: instrument?.region ?? null,
          assetCurrency: instrument?.assetCurrency ?? null,
        };
      });
    const cash = this.totals().cashParts.map((part) => ({
      currency: part.currency,
      value: part.amount * (this.fx.rate(part.currency, currency) ?? 0),
    }));

    return exposureBreakdownsOf(holdings, cash);
  });

  readonly staleQuotes = computed(() =>
    this.sleeves().some((sleeve) => sleeve.positions.some((position) => position.isStale)),
  );

  private rateToViewCurrency(currency: string): number {
    return currency === this.currency() ? 1 : (this.fx.rate(currency, this.currency()) ?? 1);
  }

  private setupOf(holdings: ValuedHoldingsDto, rate: number): SleeveSetup {
    const account = this.accounts
      .accounts()
      .find((candidate) => candidate.id === holdings.accountId);
    const targetShares = new Map(
      (account?.subPortfolios ?? []).map((sub) => [sub.id, sub.targetShare] as const),
    );

    return { accountName: this.isAll() ? (account?.name ?? '') : null, targetShares, rate };
  }

  private cashPartsInView(): { currency: string; amount: number }[] {
    const parts = new Map<string, number>();

    for (const part of this.sleeves().flatMap((sleeve) => sleeve.cashParts))
      parts.set(part.currency, (parts.get(part.currency) ?? 0) + part.amount);

    return [...parts].map(([currency, amount]) => ({ currency, amount }));
  }

  private accountIdsInView(): string[] {
    if (this.accounts.isAll()) return this.accounts.accounts().map((account) => account.id);

    const currentId = this.accounts.currentId();

    return currentId ? [currentId] : [];
  }
}

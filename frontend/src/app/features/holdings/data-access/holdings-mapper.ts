import { inject, Injectable } from '@angular/core';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { HoldingsTotalsVm, PositionVm, SleeveVm } from '../models/holdings-vm';
import {
  ValuedHoldingsDto,
  ValuedPositionDto,
  ValuedSubPortfolioDto,
} from '../models/valued-holdings-dto';

export interface SleeveSetup {
  accountName: string | null;
  targetShares: Map<string, number>;
  rate: number;
}

@Injectable({ providedIn: 'root' })
export class HoldingsMapper {
  private readonly instruments = inject(InstrumentCatalog);

  sleevesOf(holdings: ValuedHoldingsDto, setup: SleeveSetup): SleeveVm[] {
    return holdings.subPortfolios.map((sub) => this.sleeveOf(sub, holdings.currency, setup));
  }

  totalsOf(holdings: ValuedHoldingsDto, rate: number): HoldingsTotalsVm {
    return {
      value: holdings.value * rate,
      cash: holdings.cash * rate,
      costBasis: holdings.costBasis * rate,
      pnl: holdings.result * rate,
    };
  }

  private sleeveOf(sub: ValuedSubPortfolioDto, currency: string, setup: SleeveSetup): SleeveVm {
    return {
      id: sub.subPortfolioId,
      name: sub.name,
      accountName: setup.accountName,
      targetShare: setup.targetShares.get(sub.subPortfolioId) ?? 0,
      actualShare: sub.share,
      cash: sub.cash * setup.rate,
      cashParts: sub.cashBalances,
      value: sub.value * setup.rate,
      costBasis: sub.costBasis * setup.rate,
      pnl: sub.result * setup.rate,
      positions: sub.positions.map((position) => this.positionOf(position, currency, setup.rate)),
    };
  }

  private positionOf(position: ValuedPositionDto, currency: string, rate: number): PositionVm {
    const id = position.instrumentId;

    return {
      instrumentId: id,
      symbol: this.instruments.symbol(id),
      name: this.instruments.name(id),
      description: this.instruments.byId().get(id)?.description ?? null,
      quantity: position.quantity,
      averageCost: position.averageCost * rate,
      costBasis: position.costBasis * rate,
      price: position.price === null ? null : position.price * rate,
      quotedPrice: position.quotedPrice,
      quoteCurrency: position.quoteCurrency ?? currency,
      priceDate: position.priceDate,
      isStale: position.isStale,
      value: position.value * rate,
      pnl: position.result * rate,
      pnlPct: position.resultShare,
      weight: position.weight,
      lots: position.lots.map((lot) => ({ ...lot, unitCost: lot.unitCost * rate })),
    };
  }
}

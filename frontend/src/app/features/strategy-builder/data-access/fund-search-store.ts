import { inject, Injectable, signal } from '@angular/core';
import { toProblemMessage } from '../../../core/api/api-client';
import { AddedFundsDto } from '../models/added-funds-dto';
import { FoundFundDto } from '../models/found-fund-dto';
import { FundNotAddedDto } from '../models/fund-not-added-dto';
import { FundSearchCriteriaDto } from '../models/fund-search-criteria-dto';
import { FundSearchOptionsDto } from '../models/fund-search-options-dto';
import { StrategyBuilderApi } from './strategy-builder-api';

@Injectable()
export class FundSearchStore {
  private readonly api = inject(StrategyBuilderApi);
  readonly options = signal<FundSearchOptionsDto | null>(null);
  readonly funds = signal<FoundFundDto[] | null>(null);
  readonly selected = signal<FoundFundDto[]>([]);
  readonly failed = signal<FundNotAddedDto[]>([]);
  readonly loadingOptions = signal(false);
  readonly searching = signal(false);
  readonly adding = signal(false);
  readonly error = signal<string | null>(null);

  async loadOptions(): Promise<void> {
    if (this.options() || this.loadingOptions()) return;
    this.loadingOptions.set(true);
    this.error.set(null);

    try {
      this.options.set(await this.api.fundSearchOptions());
    } catch (error) {
      this.error.set(toProblemMessage(error));
    } finally {
      this.loadingOptions.set(false);
    }
  }

  async search(criteria: FundSearchCriteriaDto): Promise<void> {
    if (this.searching() || this.adding()) return;
    this.searching.set(true);
    this.error.set(null);
    this.failed.set([]);
    this.selected.set([]);
    this.funds.set(null);

    try {
      this.funds.set(await this.api.searchFunds(criteria));
    } catch (error) {
      this.error.set(toProblemMessage(error));
    } finally {
      this.searching.set(false);
    }
  }

  async add(selectedInstrumentIds: string[]): Promise<AddedFundsDto | null> {
    if (this.adding() || this.searching() || this.selected().length === 0) return null;
    this.adding.set(true);
    this.error.set(null);
    this.failed.set([]);

    try {
      const added = await this.api.addFunds(this.selected(), selectedInstrumentIds);
      this.failed.set(added.failed);
      const failedSymbols = new Set(added.failed.map((fund) => fund.symbol));
      const remaining = this.selected().filter((fund) => failedSymbols.has(fund.symbol));
      this.funds.set(remaining);
      this.selected.set(remaining);

      return added;
    } catch (error) {
      this.error.set(toProblemMessage(error));

      return null;
    } finally {
      this.adding.set(false);
    }
  }
}

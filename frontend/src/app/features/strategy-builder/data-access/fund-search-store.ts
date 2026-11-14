import { inject, Injectable, signal } from '@angular/core';
import { toProblemMessage } from '../../../core/api/api-client';
import { FoundFundDto } from '../models/found-fund-dto';
import { FundSearchCriteriaDto } from '../models/fund-search-criteria-dto';
import { FundSearchOptionsDto } from '../models/fund-search-options-dto';
import { StrategyBuilderApi } from './strategy-builder-api';

@Injectable()
export class FundSearchStore {
  private readonly api = inject(StrategyBuilderApi);
  readonly options = signal<FundSearchOptionsDto | null>(null);
  readonly funds = signal<FoundFundDto[] | null>(null);
  readonly loadingOptions = signal(false);
  readonly searching = signal(false);
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
    if (this.searching()) return;
    this.searching.set(true);
    this.error.set(null);
    this.funds.set(null);

    try {
      this.funds.set(await this.api.searchFunds(criteria));
    } catch (error) {
      this.error.set(toProblemMessage(error));
    } finally {
      this.searching.set(false);
    }
  }
}

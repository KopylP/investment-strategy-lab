import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import {
  DraftChangeDto,
  DraftCheckDto,
  DraftDto,
  DraftRulesDto,
  DraftSummaryDto,
  LayoutDto,
} from '../models/draft-dto';
import { InstrumentChoiceDto } from '../models/instrument-dto';
import { VocabularyDto } from '../models/vocabulary-dto';

const base = '/api/strategy-builder';

@Injectable({ providedIn: 'root' })
export class StrategyBuilderApi {
  private readonly api = inject(ApiClient);

  vocabulary(): Promise<VocabularyDto> {
    return this.api.get<VocabularyDto>(`${base}/vocabulary`);
  }

  instruments(): Promise<InstrumentChoiceDto[]> {
    return this.api.get<InstrumentChoiceDto[]>(`${base}/instruments`);
  }

  drafts(): Promise<DraftSummaryDto[]> {
    return this.api.get<DraftSummaryDto[]>(`${base}/drafts`);
  }

  create(name: string, template: string | null): Promise<DraftDto> {
    return this.api.post<DraftDto>(`${base}/drafts`, { name, template });
  }

  get(id: string): Promise<DraftDto> {
    return this.api.get<DraftDto>(`${base}/drafts/${id}`);
  }

  change(id: string, change: DraftChangeDto): Promise<DraftDto> {
    return this.api.put<DraftDto>(`${base}/drafts/${id}`, change);
  }

  moveNodes(id: string, layout: LayoutDto): Promise<void> {
    return this.api.put(`${base}/drafts/${id}/layout`, layout);
  }

  check(id: string): Promise<DraftCheckDto> {
    return this.api.post<DraftCheckDto>(`${base}/drafts/${id}/check`);
  }

  copyEntryToExit(id: string, revision: number, margin: number): Promise<DraftDto> {
    return this.api.post<DraftDto>(`${base}/drafts/${id}/copy-entry-to-exit`, { revision, margin });
  }

  rules(id: string): Promise<DraftRulesDto> {
    return this.api.get<DraftRulesDto>(`${base}/drafts/${id}/rules`);
  }

  importRules(name: string, json: string): Promise<DraftDto> {
    return this.api.post<DraftDto>(`${base}/drafts/import`, { name, json });
  }

  discard(id: string): Promise<void> {
    return this.api.delete(`${base}/drafts/${id}`);
  }
}

import { OwnSignalDto } from '../models/own-signal-dto';
import { PublishedStrategyDto } from '../models/published-strategy-dto';
import { PublishedVersionDto } from '../models/published-version-dto';
import { TrialRunDto } from '../models/trial-run-dto';
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

  signals(): Promise<OwnSignalDto[]> {
    return this.api.get<OwnSignalDto[]>(`${base}/signals`);
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

  trials(id: string): Promise<TrialRunDto[]> {
    return this.api.get<TrialRunDto[]>(`${base}/drafts/${id}/trials`);
  }

  runTrial(id: string, label: string | null = null): Promise<TrialRunDto> {
    return this.api.post<TrialRunDto>(`${base}/drafts/${id}/trials`, { label });
  }

  publish(id: string, revision: number): Promise<PublishedVersionDto> {
    return this.api.post<PublishedVersionDto>(`${base}/drafts/${id}/publish`, { revision });
  }

  published(id: string, version: number): Promise<PublishedStrategyDto> {
    return this.api.get<PublishedStrategyDto>(`${base}/published/${id}/versions/${version}`);
  }

  openPublished(id: string, version: number): Promise<DraftDto> {
    return this.api.post<DraftDto>(`${base}/published/${id}/versions/${version}/draft`);
  }

  discard(id: string): Promise<void> {
    return this.api.delete(`${base}/drafts/${id}`);
  }
}

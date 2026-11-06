import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { ExperimentDto } from '../models/experiment-dto';
import { JournalPageDto } from '../models/journal-page-dto';

const journalPageSize = 50;

@Injectable({ providedIn: 'root' })
export class ExperimentsApi {
  private readonly api = inject(ApiClient);

  get(id: string): Promise<ExperimentDto> {
    return this.api.get<ExperimentDto>(`/api/backtesting/experiments/${id}`);
  }

  journal(id: string, filter: { offset: number; kind: string | null; from: string | null; to: string | null }): Promise<JournalPageDto> {
    return this.api.get<JournalPageDto>(`/api/backtesting/experiments/${id}/journal`, { ...filter, limit: journalPageSize });
  }
}

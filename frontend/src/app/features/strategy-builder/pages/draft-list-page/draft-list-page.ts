import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { DateTimePipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { DraftsStore } from '../../data-access/drafts-store';
import { NewDraftDialog } from '../../ui/new-draft-dialog/new-draft-dialog';

@Component({
  selector: 'app-builder-draft-list-page',
  imports: [
    Button,
    Message,
    Skeleton,
    TableModule,
    DateTimePipe,
    EmptyState,
    PageHeader,
    NewDraftDialog,
  ],
  providers: [DraftsStore],
  templateUrl: './draft-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftListPage {
  protected readonly store = inject(DraftsStore);
  protected readonly router = inject(Router);
  protected readonly newDraftVisible = signal(
    inject(ActivatedRoute).snapshot.queryParamMap.get('new') === '1',
  );

  protected async create(request: { name: string; template: string | null }): Promise<void> {
    const draft = await this.store.create(request.name, request.template);
    if (!draft) return;

    this.newDraftVisible.set(false);
    await this.router.navigate(['/strategy-builder', draft.id]);
  }

  protected async importFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    const name = file.name.replace(/\.json$/i, '') || 'Імпортована стратегія';
    const draft = await this.store.importRules(name, await file.text());
    if (draft) await this.router.navigate(['/strategy-builder', draft.id]);
  }
}

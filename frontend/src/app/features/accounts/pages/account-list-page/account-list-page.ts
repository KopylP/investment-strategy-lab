import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { MeterGroup } from 'primeng/metergroup';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { AccountContext } from '../../../../core/context/account-context';
import { AccountSetup } from '../../../../shared/models/api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

const palette = [
  'var(--p-primary-500)',
  'var(--p-amber-500)',
  'var(--p-teal-500)',
  'var(--p-pink-500)',
  'var(--p-sky-500)',
];

@Component({
  selector: 'app-account-list-page',
  imports: [RouterLink, Button, MeterGroup, Skeleton, Tag, PageHeader, EmptyState],
  templateUrl: './account-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountListPage {
  protected readonly accounts = inject(AccountContext);

  protected meter(account: AccountSetup) {
    return account.subPortfolios
      .filter((sub) => sub.status === 'Active')
      .map((sub, index) => ({
        label: sub.name,
        value: Math.round(sub.targetShare * 1000) / 10,
        color: palette[index % palette.length],
      }));
  }
}

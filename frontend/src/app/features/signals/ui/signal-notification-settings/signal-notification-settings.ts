import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Checkbox } from 'primeng/checkbox';
import { Skeleton } from 'primeng/skeleton';
import { SignalsStore } from '../../data-access/signals-store';

@Component({
  selector: 'app-signal-notification-settings',
  imports: [FormsModule, Checkbox, Skeleton],
  providers: [SignalsStore],
  template: `
    <div class="card max-w-2xl">
      <h2 class="text-lg font-semibold mb-1">Про які сигнали сповіщати</h2>
      <p class="text-muted-color mb-4">Новий сигнал сповіщає сам, поки його тут не вимкнете.</p>

      @if (store.isLoading() && !store.signals().length) {
        <p-skeleton height="4rem" />
      } @else {
        <ul class="flex flex-col gap-3 list-none p-0 m-0">
          @for (item of store.signals(); track item.id) {
            <li class="flex items-center gap-3 min-h-11">
              <p-checkbox
                [inputId]="'notify-' + item.id"
                [binary]="true"
                [ngModel]="store.notifies(item.id)"
                (ngModelChange)="store.setNotify(item.id, $event)"
                [disabled]="store.saving()"
              />
              <label [for]="'notify-' + item.id" class="flex-1">{{ item.name }}</label>
            </li>
          } @empty {
            <li class="text-muted-color">Сигналів поки немає.</li>
          }
        </ul>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignalNotificationSettings {
  protected readonly store = inject(SignalsStore);
}

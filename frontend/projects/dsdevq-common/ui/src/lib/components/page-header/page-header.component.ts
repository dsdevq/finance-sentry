import {ChangeDetectionStrategy, Component, input, output} from '@angular/core';

import {ButtonComponent} from '../button/button.component';
import {type LucideIconName} from '../icon/icon.component';

@Component({
  selector: 'cmn-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonComponent],
  host: {style: 'display: block'},
  template: `
    <div class="flex items-start justify-between gap-cmn-4">
      <div class="flex flex-col gap-cmn-1">
        <h1 class="font-headline text-cmn-2xl font-semibold text-text-primary">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="font-body text-cmn-sm text-text-secondary">{{ subtitle() }}</p>
        }
      </div>
      @if (actionLabel()) {
        <cmn-button
          [icon]="actionIcon()"
          [loading]="actionLoading()"
          [disabled]="actionDisabled()"
          (clicked)="actionClick.emit()"
          variant="primary"
        >
          {{ actionLabel() }}
        </cmn-button>
      }
    </div>
  `,
})
export class PageHeaderComponent {
  public readonly title = input.required<string>();
  public readonly subtitle = input<string>('');
  public readonly actionLabel = input<string | null>(null);
  public readonly actionIcon = input<LucideIconName | null>(null);
  public readonly actionLoading = input<boolean>(false);
  public readonly actionDisabled = input<boolean>(false);

  public readonly actionClick = output<void>();
}

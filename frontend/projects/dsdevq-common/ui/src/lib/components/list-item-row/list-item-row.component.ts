import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

@Component({
  selector: 'cmn-list-item-row',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {style: 'display: block'},
  template: `
    <div [class]="rowClasses()">
      <ng-content select="[avatar]" />
      <div class="flex-1 min-w-0">
        <div class="font-label text-cmn-sm font-medium text-text-primary truncate">
          {{ label() }}
        </div>
        @if (sublabel()) {
          <div class="text-cmn-xs text-text-secondary truncate">{{ sublabel() }}</div>
        }
      </div>
      <ng-content select="[meta]" />
      <ng-content select="[amount]" />
      <ng-content select="[actions]" />
    </div>
  `,
})
export class ListItemRowComponent {
  public readonly label = input.required<string>();
  public readonly sublabel = input<string | null>(null);
  public readonly dimmed = input<boolean>(false);

  protected readonly rowClasses = computed(() => {
    const base =
      'flex items-center gap-cmn-3 border-b border-border-default px-cmn-4 py-cmn-3 last:border-b-0 hover:bg-surface-bg transition-colors';
    return this.dimmed() ? `${base} opacity-55` : base;
  });
}

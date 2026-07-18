import {ChangeDetectionStrategy, Component, input, model} from '@angular/core';

export interface CmnTab {
  id: string;
  label: string;
}

const TAB_BASE = 'px-cmn-4 py-cmn-2 text-cmn-sm font-medium transition-colors border-b-2';
const TAB_ACTIVE = 'text-text-primary border-accent-default';
const TAB_INACTIVE = 'text-text-secondary border-transparent hover:text-text-primary';

@Component({
  selector: 'cmn-tab-group',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {style: 'display: block'},
  template: `
    <div class="flex gap-cmn-2 border-b border-border-default" role="tablist">
      @for (tab of tabs(); track tab.id) {
        <button
          [attr.aria-selected]="tab.id === activeTab()"
          [class]="tabClass(tab.id)"
          (click)="selectTab(tab.id)"
          type="button"
          role="tab"
        >
          {{ tab.label }}
        </button>
      }
    </div>
    <ng-content />
  `,
})
export class TabGroupComponent {
  public readonly tabs = input.required<CmnTab[]>();
  public readonly activeTab = model<string>('');

  protected tabClass(id: string): string {
    return id === this.activeTab() ? `${TAB_BASE} ${TAB_ACTIVE}` : `${TAB_BASE} ${TAB_INACTIVE}`;
  }

  protected selectTab(id: string): void {
    this.activeTab.set(id);
  }
}

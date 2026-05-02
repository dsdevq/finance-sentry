import {DialogRef} from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';

import {CMN_DIALOG_DATA} from '../dialog/dialog-config';
import {type PaletteResult, type CommandPaletteItem} from './command-palette-item.model';
import {IconComponent} from '../icon/icon.component';

interface PaletteGroup {
  group: string;
  items: {item: CommandPaletteItem; idx: number}[];
}

@Component({
  selector: 'cmn-command-palette',
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      @keyframes cmn-fade-in {
        from {
          opacity: 0;
        }
        to {
          opacity: 1;
        }
      }
      @keyframes cmn-slide-in {
        from {
          opacity: 0;
          transform: translateY(-8px) scale(0.98);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }
      .cmn-palette-backdrop {
        animation: cmn-fade-in 0.12s ease both;
      }
      .cmn-palette-panel {
        animation: cmn-slide-in 0.16s ease both;
      }
    `,
  ],
  template: `
    <div
      (click)="close()"
      class="fixed inset-0 z-[2000] flex items-start justify-center"
      style="padding-top: 18vh"
    >
      <div class="cmn-palette-backdrop absolute inset-0 bg-black/45 backdrop-blur-sm"></div>

      <div
        (click)="$event.stopPropagation()"
        class="cmn-palette-panel relative z-10 w-full max-w-[560px] overflow-hidden rounded-2xl border border-border-default bg-surface-card shadow-cmn-md"
      >
        <!-- Search row -->
        <div class="flex items-center gap-cmn-3 border-b border-border-default px-cmn-4 py-3.5">
          <cmn-icon name="Search" size="sm" class="shrink-0 text-text-secondary" />
          <input
            #searchInput
            [value]="query()"
            (input)="query.set($any($event.target).value)"
            placeholder="Search pages, actions…"
            class="flex-1 border-none bg-transparent text-[15px] text-text-primary outline-none placeholder:text-text-disabled"
          />
          <kbd
            class="shrink-0 rounded border border-border-default px-1.5 py-0.5 font-mono text-cmn-xs text-text-disabled"
            >ESC</kbd
          >
        </div>

        <!-- Results -->
        <div class="max-h-[360px] overflow-y-auto py-1.5">
          @if (filteredItems().length === 0) {
            <div class="px-cmn-6 py-cmn-6 text-center text-cmn-sm text-text-disabled">
              No results for "{{ query() }}"
            </div>
          } @else {
            @for (group of groupedItems(); track group.group) {
              <div>
                <div
                  class="px-cmn-4 pb-1 pt-1.5 text-[10px] font-bold uppercase tracking-[0.08em] text-text-disabled"
                >
                  {{ group.group }}
                </div>
                @for (entry of group.items; track entry.item.id) {
                  <div
                    [class.bg-accent-subtle]="entry.idx === selectedIndex()"
                    (click)="activate(entry.item)"
                    (mouseenter)="selectedIndex.set(entry.idx)"
                    class="flex cursor-pointer items-center gap-cmn-3 px-cmn-4 py-2.5 transition-colors duration-75"
                  >
                    <div
                      [class.bg-accent-default]="entry.idx === selectedIndex()"
                      [class.bg-surface-raised]="entry.idx !== selectedIndex()"
                      class="flex h-[30px] w-[30px] shrink-0 items-center justify-center rounded-cmn-md transition-colors duration-75"
                    >
                      <cmn-icon
                        [color]="entry.idx === selectedIndex() ? 'white' : 'var(--color-text-secondary)'"
                        [name]="entry.item.icon"
                        size="sm"
                      />
                    </div>
                    <span
                      [class.font-medium]="entry.idx === selectedIndex()"
                      [class.text-accent-default]="entry.idx === selectedIndex()"
                      [class.text-text-primary]="entry.idx !== selectedIndex()"
                      class="text-[14px] transition-colors duration-75"
                      >{{ entry.item.label }}</span
                    >
                    @if (entry.idx === selectedIndex()) {
                      <div class="ml-auto">
                        <kbd
                          class="rounded border border-border-default px-1.5 py-0.5 font-mono text-[10px] text-text-disabled"
                          >↵</kbd
                        >
                      </div>
                    }
                  </div>
                }
              </div>
            }
          }
        </div>

        <!-- Footer hints -->
        <div class="flex gap-3.5 border-t border-border-default px-cmn-4 py-2">
          @for (hint of KEY_HINTS; track hint[0]) {
            <div class="flex items-center gap-1">
              <kbd
                class="rounded border border-border-default px-1 py-0.5 font-mono text-[10px] text-text-disabled"
                >{{ hint[0] }}</kbd
              >
              <span class="text-[11px] text-text-disabled">{{ hint[1] }}</span>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  host: {
    '(window:keydown)': 'onKeyDown($event)',
  }
})
export class CommandPaletteComponent {
  protected readonly KEY_HINTS: [string, string][] = [
    ['↑↓', 'Navigate'],
    ['↵', 'Select'],
    ['ESC', 'Close'],
  ];

  private static readonly FOCUS_DELAY_MS = 30;

  private readonly dialogRef = inject<DialogRef<PaletteResult>>(DialogRef);
  private readonly items = inject<CommandPaletteItem[]>(CMN_DIALOG_DATA);
  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  protected readonly query = signal('');
  protected readonly selectedIndex = signal(0);

  protected readonly filteredItems = computed(() => {
    const q = this.query().toLowerCase();
    return this.items.filter(
      i => i.label.toLowerCase().includes(q) || i.group.toLowerCase().includes(q)
    );
  });

  protected readonly groupedItems = computed<PaletteGroup[]>(() => {
    let globalIdx = 0;
    const map = new Map<string, {item: CommandPaletteItem; idx: number}[]>();
    for (const item of this.filteredItems()) {
      const bucket = map.get(item.group) ?? [];
      bucket.push({item, idx: globalIdx++});
      map.set(item.group, bucket);
    }
    return Array.from(map.entries()).map(([group, items]) => ({group, items}));
  });

  constructor() {
    effect(
      () => {
        void this.filteredItems().length;
        this.selectedIndex.set(0);
      },
      {allowSignalWrites: true}
    );

    setTimeout(
      () => this.searchInput()?.nativeElement.focus(),
      CommandPaletteComponent.FOCUS_DELAY_MS
    );
  }

  protected onKeyDown(e: KeyboardEvent): void {
    const items = this.filteredItems();
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.selectedIndex.update(s => Math.min(s + 1, items.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.selectedIndex.update(s => Math.max(s - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      this.activate(items[this.selectedIndex()]);
    } else if (e.key === 'Escape') {
      this.close();
    }
  }

  protected close(): void {
    this.dialogRef.close();
  }

  protected activate(item: CommandPaletteItem | undefined): void {
    if (!item) { return; }
    this.dialogRef.close({
      type: item.id.startsWith('_') ? 'action' : 'navigate',
      id: item.id,
    });
  }
}

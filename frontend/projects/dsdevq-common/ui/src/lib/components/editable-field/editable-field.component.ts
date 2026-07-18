import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';

export type EditableFieldType = 'text' | 'email' | 'number' | 'tel' | 'url';

const VIEW_CLASSES =
  'inline-flex min-h-cmn-10 w-full items-center justify-between gap-cmn-3 rounded-cmn-md ' +
  'border border-transparent px-cmn-3 py-cmn-2 text-left text-cmn-sm text-text-primary ' +
  'transition-colors hover:border-border-default hover:bg-surface-raised ' +
  'focus:outline-none focus:ring-2 focus:ring-border-focus focus:ring-offset-1';

const EMPTY_CLASSES = 'text-text-secondary';

const INPUT_CLASSES =
  'min-h-cmn-10 w-full rounded-cmn-md border border-border-default bg-surface-card ' +
  'px-cmn-3 py-cmn-2 text-cmn-sm text-text-primary placeholder:text-text-disabled ' +
  'focus:border-border-focus focus:outline-none focus:ring-2 focus:ring-border-focus';

const ACTION_CLASSES =
  'inline-flex min-h-cmn-10 items-center rounded-cmn-md border border-border-default ' +
  'px-cmn-3 py-cmn-2 text-cmn-sm font-medium text-text-primary transition-colors ' +
  'hover:bg-surface-raised focus:outline-none focus:ring-2 focus:ring-border-focus focus:ring-offset-1';

@Component({
  selector: 'cmn-editable-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {style: 'display: block'},
  template: `
    @if (editing()) {
      <div class="flex items-center gap-cmn-2">
        <input
          [type]="type()"
          [value]="draft()"
          [placeholder]="placeholder()"
          [attr.aria-label]="ariaLabel()"
          [class]="inputClasses"
          (input)="updateDraft($event)"
          (keydown.enter)="save()"
          (keydown.escape)="cancel()"
        />
        <button [class]="actionClasses" (click)="save()" type="button">Save</button>
        <button [class]="actionClasses" (click)="cancel()" type="button">Cancel</button>
      </div>
    } @else {
      <button
        [class]="viewClasses()"
        [disabled]="disabled()"
        [attr.aria-label]="editLabel()"
        (click)="startEditing()"
        type="button"
      >
        <span [class]="displayClass()">{{ displayValue() }}</span>
        <span class="text-cmn-xs font-medium text-text-secondary">Edit</span>
      </button>
    }
  `,
})
export class EditableFieldComponent {
  public readonly value = model<string>('');
  public readonly placeholder = input<string>('Add value');
  public readonly emptyLabel = input<string>('Not set');
  public readonly ariaLabel = input<string>('Editable field');
  public readonly type = input<EditableFieldType>('text');
  public readonly disabled = input<boolean>(false);

  public readonly saved = output<string>();
  public readonly cancelled = output<void>();

  protected readonly editing = signal<boolean>(false);
  protected readonly draft = signal<string>('');
  protected readonly inputClasses = INPUT_CLASSES;
  protected readonly actionClasses = ACTION_CLASSES;

  protected readonly displayValue = computed(() => this.value().trim() || this.emptyLabel());
  protected readonly displayClass = computed(() => (this.value().trim() ? '' : EMPTY_CLASSES));
  protected readonly viewClasses = computed(() =>
    this.disabled() ? `${VIEW_CLASSES} opacity-50 cursor-not-allowed` : VIEW_CLASSES
  );
  protected readonly editLabel = computed(() => `Edit ${this.ariaLabel()}`);

  protected startEditing(): void {
    if (this.disabled()) {
      return;
    }
    this.draft.set(this.value());
    this.editing.set(true);
  }

  protected updateDraft(event: Event): void {
    this.draft.set((event.target as HTMLInputElement).value);
  }

  protected save(): void {
    const nextValue = this.draft().trim();
    this.value.set(nextValue);
    this.editing.set(false);
    this.saved.emit(nextValue);
  }

  protected cancel(): void {
    this.draft.set(this.value());
    this.editing.set(false);
    this.cancelled.emit();
  }
}

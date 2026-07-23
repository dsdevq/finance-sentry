import {
  ChangeDetectionStrategy,
  Component,
  computed,
  forwardRef,
  input,
  signal,
} from '@angular/core';
import {ControlValueAccessor, NG_VALUE_ACCESSOR} from '@angular/forms';

import {IconComponent} from '../icon/icon.component';

export type SelectSize = 'sm' | 'md' | 'lg';
export type SelectMode = 'default' | 'multiple';
export type SelectOptionValue = string | number;
export type SelectValue = SelectOptionValue | SelectOptionValue[] | null;

export interface SelectOption {
  label: string;
  value: SelectOptionValue;
  disabled?: boolean;
}

const SIZE_CLASSES: Record<SelectSize, string> = {
  sm: 'py-1 text-cmn-xs',
  md: 'py-1.5 text-cmn-sm',
  lg: 'py-2 text-cmn-base',
};

const ICON_SIZE: Record<SelectSize, 'sm' | 'md' | 'lg'> = {
  sm: 'sm',
  md: 'sm',
  lg: 'md',
};

/**
 * Native-backed select. Deliberately built on a plain <select> + design tokens rather than a
 * third-party widget: it needs no external stylesheet or icon registration, stays consistent
 * with the app's Tailwind theme, and gets accessibility + keyboard type-ahead for free.
 */
@Component({
  selector: 'cmn-select',
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectComponent),
      multi: true,
    },
  ],
  template: `
    <div class="cmn-select relative w-full">
      <select
        #native
        [class]="selectClasses()"
        [disabled]="disabled()"
        [multiple]="mode() === 'multiple'"
        [attr.aria-invalid]="hasError() ? 'true' : null"
        (change)="onNativeChange($event)"
        (blur)="onBlur()"
      >
        @if (mode() !== 'multiple') {
          <option [selected]="isEmpty()" [disabled]="!allowClear()" value="">
            {{ placeholder() || 'Select…' }}
          </option>
        }
        @for (option of options(); track option.value) {
          <option
            [value]="option.value"
            [selected]="isSelected(option.value)"
            [disabled]="option.disabled ?? false"
          >
            {{ option.label }}
          </option>
        }
      </select>
      @if (mode() !== 'multiple') {
        <cmn-icon
          [size]="iconSize()"
          name="ChevronDown"
          class="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2 text-text-secondary"
        />
      }
    </div>
  `,
})
export class SelectComponent implements ControlValueAccessor {
  public readonly options = input<readonly SelectOption[]>([]);
  public readonly placeholder = input<string>('');
  public readonly size = input<SelectSize>('md');
  public readonly mode = input<SelectMode>('default');
  public readonly allowClear = input<boolean>(false);
  // Kept for API compatibility; native selects have built-in keyboard type-ahead.
  public readonly showSearch = input<boolean>(false);
  public readonly hasError = input<boolean>(false);

  public readonly iconSize = computed(() => ICON_SIZE[this.size()]);
  public readonly selectClasses = computed(() =>
    [
      'block w-full appearance-none rounded border bg-surface-bg pl-cmn-2 pr-8',
      'text-text-primary outline-none transition-colors',
      'focus:border-accent-default disabled:cursor-not-allowed disabled:opacity-60',
      this.hasError() ? 'border-error-default' : 'border-surface-raised',
      SIZE_CLASSES[this.size()],
      this.mode() === 'multiple' ? 'pr-cmn-2' : '',
    ].join(' ')
  );

  protected readonly value = signal<SelectValue>(null);
  protected readonly disabled = signal<boolean>(false);
  protected readonly isEmpty = computed(() => {
    const v = this.value();
    return v === null || v === undefined || v === '';
  });

  private onChange: (value: SelectValue) => void = (_: SelectValue) => void 0;
  private onTouched: () => void = () => void 0;

  public writeValue(value: SelectValue | undefined): void {
    this.value.set(value ?? null);
  }

  public registerOnChange(fn: (value: SelectValue) => void): void {
    this.onChange = fn;
  }

  public registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  public setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected isSelected(optionValue: SelectOptionValue): boolean {
    const v = this.value();
    return Array.isArray(v) ? v.includes(optionValue) : v === optionValue;
  }

  protected onNativeChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    let next: SelectValue;
    if (this.mode() === 'multiple') {
      next = Array.from(target.selectedOptions).map(o => o.value);
    } else {
      next = target.value === '' ? null : target.value;
    }
    this.value.set(next);
    this.onChange(next);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}

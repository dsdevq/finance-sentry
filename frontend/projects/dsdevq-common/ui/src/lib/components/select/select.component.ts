import {
  ChangeDetectionStrategy,
  Component,
  computed,
  forwardRef,
  input,
  signal,
} from '@angular/core';
import {ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR} from '@angular/forms';
import {NzSelectModule} from 'ng-zorro-antd/select';

export type SelectSize = 'sm' | 'md' | 'lg';
export type SelectMode = 'default' | 'multiple';
export type SelectOptionValue = string | number;
export type SelectValue = SelectOptionValue | SelectOptionValue[] | null;

export interface SelectOption {
  label: string;
  value: SelectOptionValue;
  disabled?: boolean;
}

const BASE_CLASSES = 'cmn-select block w-full';

const SIZE_CLASSES: Record<SelectSize, string> = {
  sm: 'cmn-select--sm',
  md: 'cmn-select--md',
  lg: 'cmn-select--lg',
};

const NZ_SIZE: Record<SelectSize, 'small' | 'default' | 'large'> = {
  sm: 'small',
  md: 'default',
  lg: 'large',
};

@Component({
  selector: 'cmn-select',
  imports: [FormsModule, NzSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectComponent),
      multi: true,
    },
  ],
  template: `
    <nz-select
      [ngModel]="value()"
      [nzAllowClear]="allowClear()"
      [nzDisabled]="disabled()"
      [nzMode]="mode()"
      [nzPlaceHolder]="placeholder()"
      [nzShowSearch]="showSearch()"
      [nzSize]="nzSize()"
      [nzStatus]="hasError() ? 'error' : ''"
      [class]="classes()"
      (ngModelChange)="onValueChange($event)"
      (nzBlur)="onBlur()"
    >
      @for (option of options(); track option.value) {
        <nz-option
          [nzDisabled]="option.disabled ?? false"
          [nzLabel]="option.label"
          [nzValue]="option.value"
        />
      }
    </nz-select>
  `,
})
export class SelectComponent implements ControlValueAccessor {
  public readonly options = input<readonly SelectOption[]>([]);
  public readonly placeholder = input<string>('');
  public readonly size = input<SelectSize>('md');
  public readonly mode = input<SelectMode>('default');
  public readonly allowClear = input<boolean>(false);
  public readonly showSearch = input<boolean>(false);
  public readonly hasError = input<boolean>(false);

  public readonly classes = computed(() => [BASE_CLASSES, SIZE_CLASSES[this.size()]].join(' '));
  public readonly nzSize = computed(() => NZ_SIZE[this.size()]);

  protected readonly value = signal<SelectValue>(null);
  protected readonly disabled = signal<boolean>(false);

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

  protected onValueChange(value: SelectValue): void {
    this.value.set(value);
    this.onChange(value);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}

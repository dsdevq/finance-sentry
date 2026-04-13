import {
  AfterContentInit,
  ChangeDetectionStrategy,
  Component,
  contentChild,
  forwardRef,
  input,
} from '@angular/core';
import {ControlValueAccessor, NG_VALUE_ACCESSOR} from '@angular/forms';

import {InputComponent} from '../input/input.component';

let fieldCounter = 0;

@Component({
  selector: 'cmn-form-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => FormFieldComponent),
      multi: true,
    },
  ],
  template: `
    <div class="flex flex-col gap-cmn-1">
      @if (label()) {
        <label [for]="fieldId" class="font-label text-cmn-sm font-medium text-text-primary">
          {{ label() }}
          @if (required()) {
            <span class="text-status-error ml-0.5" aria-hidden="true">*</span>
          }
        </label>
      }

      <ng-content />

      @if (hint() && !errorMessage()) {
        <span class="text-cmn-xs text-text-secondary">{{ hint() }}</span>
      }

      @if (errorMessage()) {
        <span role="alert" aria-live="polite" class="text-cmn-xs text-status-error">{{
          errorMessage()
        }}</span>
      }
    </div>
  `,
})
export class FormFieldComponent implements ControlValueAccessor, AfterContentInit {
  // Private fields first (member-ordering)
  private pendingValue: unknown = undefined;
  private pendingDisabled: boolean | undefined = undefined;
  private onChangeFn: ((v: unknown) => void) | undefined;
  private onTouchedFn: (() => void) | undefined;

  // Public signal inputs
  public readonly label = input<string>('');
  public readonly hint = input<string>('');
  public readonly errorMessage = input<string>('');
  public readonly required = input<boolean>(false);

  public readonly fieldId: string;

  // Protected content child
  protected readonly inputChild = contentChild(InputComponent);

  constructor() {
    this.fieldId = `cmn-field-${++fieldCounter}`;
  }

  public ngAfterContentInit(): void {
    const inputCmp = this.inputChild();
    if (!inputCmp) {
      return;
    }

    inputCmp.setInputId(this.fieldId);

    if (this.pendingValue !== undefined) {
      inputCmp.writeValue(this.pendingValue as string);
    }
    if (this.pendingDisabled !== undefined) {
      inputCmp.setDisabledState(this.pendingDisabled);
    }
    if (this.onChangeFn) {
      inputCmp.registerOnChange(this.onChangeFn as (v: string) => void);
    }
    if (this.onTouchedFn) {
      inputCmp.registerOnTouched(this.onTouchedFn);
    }
  }

  public writeValue(value: unknown): void {
    this.pendingValue = value;
    this.inputChild()?.writeValue(value as string);
  }

  public registerOnChange(fn: (v: unknown) => void): void {
    this.onChangeFn = fn;
    this.inputChild()?.registerOnChange(fn as (v: string) => void);
  }

  public registerOnTouched(fn: () => void): void {
    this.onTouchedFn = fn;
    this.inputChild()?.registerOnTouched(fn);
  }

  public setDisabledState(isDisabled: boolean): void {
    this.pendingDisabled = isDisabled;
    this.inputChild()?.setDisabledState(isDisabled);
  }
}

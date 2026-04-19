import {
  AfterContentInit,
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChild,
  forwardRef,
  inject,
  input,
} from '@angular/core';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {AbstractControl, ControlValueAccessor, NG_VALUE_ACCESSOR} from '@angular/forms';
import {merge, of, switchMap} from 'rxjs';

import {VALIDATION_MESSAGES} from '../../tokens/validation-messages.token';
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

      @if (hint() && !resolvedError()) {
        <span class="text-cmn-xs text-text-secondary">{{ hint() }}</span>
      }

      @if (resolvedError()) {
        <span role="alert" aria-live="polite" class="text-cmn-xs text-status-error">{{
          resolvedError()
        }}</span>
      }
    </div>
  `,
})
export class FormFieldComponent implements ControlValueAccessor, AfterContentInit {
  private readonly validationMessages = inject(VALIDATION_MESSAGES);
  private pendingValue: unknown = undefined;
  private pendingDisabled: boolean | undefined = undefined;
  private onChangeFn: ((v: unknown) => void) | undefined;
  private onTouchedFn: (() => void) | undefined;

  public readonly label = input<string>('');
  public readonly hint = input<string>('');
  public readonly errorMessage = input<string>('');
  public readonly required = input<boolean>(false);
  public readonly control = input<AbstractControl | null>(null);

  public readonly fieldId: string;

  protected readonly inputChild = contentChild(InputComponent);
  protected readonly controlChanges = toSignal(
    toObservable(this.control).pipe(
      switchMap(ctrl => (ctrl ? merge(ctrl.statusChanges, ctrl.valueChanges) : of(null)))
    )
  );

  protected readonly resolvedError = computed(() => {
    this.controlChanges();
    if (this.errorMessage()) {
      return this.errorMessage();
    }
    const ctrl = this.control();
    if (!ctrl?.touched || !ctrl.errors) {
      return '';
    }
    const [key, params] = Object.entries(ctrl.errors)[0];
    return this.validationMessages[key]?.(params) ?? key;
  });

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

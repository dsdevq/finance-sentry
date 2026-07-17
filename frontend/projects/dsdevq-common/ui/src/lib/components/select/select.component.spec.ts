import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {SelectComponent, type SelectValue} from './select.component';

interface SelectInternals {
  value(): SelectValue;
  onValueChange(v: SelectValue): void;
  onBlur(): void;
  disabled(): boolean;
}

describe('SelectComponent', () => {
  let fixture: ComponentFixture<SelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [SelectComponent]}).compileComponents();
    fixture = TestBed.createComponent(SelectComponent);
  });

  function selectElement(): HTMLElement | null {
    return fixture.nativeElement.querySelector('nz-select');
  }

  function internals(): SelectInternals {
    return fixture.componentInstance as unknown as SelectInternals;
  }

  it('renders the configured select options', () => {
    fixture.componentRef.setInput('options', [
      {label: 'Euro', value: 'eur'},
      {label: 'US Dollar', value: 'usd', disabled: true},
    ]);
    fixture.detectChanges();

    expect(fixture.componentInstance.options()).toHaveLength(2);
    expect(selectElement()).not.toBeNull();
  });

  it('applies size classes', () => {
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    expect(selectElement()?.className).toContain('cmn-select--lg');
  });

  it('writes external form values', () => {
    fixture.componentInstance.writeValue('eur');
    fixture.detectChanges();

    expect(internals().value()).toBe('eur');
  });

  it('normalizes undefined form values to null', () => {
    fixture.componentInstance.writeValue(undefined);
    fixture.detectChanges();

    expect(internals().value()).toBeNull();
  });

  it('emits form changes when selection changes', () => {
    let emitted: SelectValue = null;
    fixture.componentInstance.registerOnChange(value => {
      emitted = value;
    });

    internals().onValueChange('usd');

    expect(emitted).toBe('usd');
    expect(internals().value()).toBe('usd');
  });

  it('marks the control as touched on blur', () => {
    let touched = false;
    fixture.componentInstance.registerOnTouched(() => {
      touched = true;
    });

    internals().onBlur();

    expect(touched).toBe(true);
  });

  it('updates disabled state from forms API', () => {
    fixture.componentInstance.setDisabledState(true);
    fixture.detectChanges();

    expect(internals().disabled()).toBe(true);
  });
});

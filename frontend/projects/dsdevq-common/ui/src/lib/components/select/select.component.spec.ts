import {ComponentFixture, TestBed} from '@angular/core/testing';

import {SelectComponent, SelectValue} from './select.component';

describe('SelectComponent', () => {
  let fixture: ComponentFixture<SelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [SelectComponent]}).compileComponents();
    fixture = TestBed.createComponent(SelectComponent);
  });

  function selectElement(): HTMLElement | null {
    return fixture.nativeElement.querySelector('nz-select');
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

    expect(fixture.componentInstance['value']()).toBe('eur');
  });

  it('normalizes undefined form values to null', () => {
    fixture.componentInstance.writeValue(undefined);
    fixture.detectChanges();

    expect(fixture.componentInstance['value']()).toBeNull();
  });

  it('emits form changes when selection changes', () => {
    let emitted: SelectValue = null;
    fixture.componentInstance.registerOnChange(value => {
      emitted = value;
    });

    fixture.componentInstance['onValueChange']('usd');

    expect(emitted).toBe('usd');
    expect(fixture.componentInstance['value']()).toBe('usd');
  });

  it('marks the control as touched on blur', () => {
    let touched = false;
    fixture.componentInstance.registerOnTouched(() => {
      touched = true;
    });

    fixture.componentInstance['onBlur']();

    expect(touched).toBe(true);
  });

  it('updates disabled state from forms API', () => {
    fixture.componentInstance.setDisabledState(true);
    fixture.detectChanges();

    expect(fixture.componentInstance['disabled']()).toBe(true);
  });
});

import {ChangeDetectionStrategy, Component} from '@angular/core';
import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {EditableFieldComponent} from './editable-field.component';

describe('EditableFieldComponent', () => {
  let fixture: ComponentFixture<EditableFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [EditableFieldComponent]}).compileComponents();
    fixture = TestBed.createComponent(EditableFieldComponent);
    fixture.componentRef.setInput('value', 'Visa ending 4421');
    fixture.componentRef.setInput('ariaLabel', 'account nickname');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have display:block on the host element', () => {
    const host = fixture.nativeElement as HTMLElement;
    expect(host.style.display).toBe('block');
  });

  it('should render the current value in view mode', () => {
    const button: HTMLButtonElement | null = fixture.nativeElement.querySelector('button');
    expect(button?.textContent).toContain('Visa ending 4421');
    expect(button?.textContent).toContain('Edit');
  });

  it('should render the empty label when the value is blank', () => {
    fixture.componentRef.setInput('value', '   ');
    fixture.componentRef.setInput('emptyLabel', 'Missing nickname');
    fixture.detectChanges();
    const button: HTMLButtonElement | null = fixture.nativeElement.querySelector('button');
    expect(button?.textContent).toContain('Missing nickname');
  });

  it('should expose a specific edit aria-label', () => {
    const button: HTMLButtonElement | null = fixture.nativeElement.querySelector('button');
    expect(button?.getAttribute('aria-label')).toBe('Edit account nickname');
  });

  it('should enter edit mode with the current value as draft', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    const input: HTMLInputElement | null = fixture.nativeElement.querySelector('input');
    expect(input?.value).toBe('Visa ending 4421');
    expect(input?.getAttribute('aria-label')).toBe('account nickname');
  });

  it('should use the configured input type and placeholder in edit mode', () => {
    fixture.componentRef.setInput('type', 'email');
    fixture.componentRef.setInput('placeholder', 'name@example.com');
    fixture.detectChanges();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    const input: HTMLInputElement | null = fixture.nativeElement.querySelector('input');
    expect(input?.type).toBe('email');
    expect(input?.placeholder).toBe('name@example.com');
  });

  it('should save a trimmed draft value', () => {
    const saved = vi.fn();
    fixture.componentInstance.saved.subscribe(saved);
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    input.value = '  Primary account  ';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    const saveButton: HTMLButtonElement = fixture.nativeElement.querySelectorAll('button')[0];
    saveButton.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.value()).toBe('Primary account');
    expect(saved).toHaveBeenCalledWith('Primary account');
    expect(fixture.nativeElement.querySelector('input')).toBeNull();
  });

  it('should save when Enter is pressed', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    input.value = 'Brokerage';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter'}));
    fixture.detectChanges();
    expect(fixture.componentInstance.value()).toBe('Brokerage');
  });

  it('should cancel edit mode and keep the prior value', () => {
    const cancelled = vi.fn();
    fixture.componentInstance.cancelled.subscribe(cancelled);
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    input.value = 'Changed';
    input.dispatchEvent(new Event('input'));
    const cancelButton: HTMLButtonElement = fixture.nativeElement.querySelectorAll('button')[1];
    cancelButton.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.value()).toBe('Visa ending 4421');
    expect(cancelled).toHaveBeenCalledOnce();
  });

  it('should cancel when Escape is pressed', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    input.value = 'Changed';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', {key: 'Escape'}));
    fixture.detectChanges();
    expect(fixture.componentInstance.value()).toBe('Visa ending 4421');
    expect(fixture.nativeElement.querySelector('input')).toBeNull();
  });

  it('should not enter edit mode when disabled', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('input')).toBeNull();
    expect(button.disabled).toBe(true);
  });
});

@Component({
  imports: [EditableFieldComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<cmn-editable-field [(value)]="label" ariaLabel="label" />',
})
class TwoWayHostComponent {
  public label = 'Initial';
}

describe('EditableFieldComponent — two-way model binding', () => {
  let fixture: ComponentFixture<TwoWayHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [TwoWayHostComponent]}).compileComponents();
    fixture = TestBed.createComponent(TwoWayHostComponent);
    fixture.detectChanges();
  });

  it('should sync the host property when a draft is saved', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    input.value = 'Updated';
    input.dispatchEvent(new Event('input'));
    const saveButton: HTMLButtonElement = fixture.nativeElement.querySelectorAll('button')[0];
    saveButton.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.label).toBe('Updated');
  });

  it('should reflect host property changes in view mode', () => {
    fixture.componentInstance.label = 'External update';
    fixture.detectChanges();
    const button: HTMLButtonElement | null = fixture.nativeElement.querySelector('button');
    expect(button?.textContent).toContain('External update');
  });
});

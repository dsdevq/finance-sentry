import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {ChatInputComponent} from './chat-input.component';

describe('ChatInputComponent', () => {
  let fixture: ComponentFixture<ChatInputComponent>;
  let component: ChatInputComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [ChatInputComponent]}).compileComponents();
    fixture = TestBed.createComponent(ChatInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  function typeInto(text: string): HTMLTextAreaElement {
    const textarea = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = text;
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    return textarea;
  }

  it('should not emit send for empty input', () => {
    let emitted: string | null = null;
    component.send.subscribe((v: string) => (emitted = v));
    typeInto('   ');
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    expect(emitted).toBeNull();
  });

  it('should emit trimmed text on submit and clear the field', () => {
    let emitted: string | null = null;
    component.send.subscribe((v: string) => (emitted = v));
    const textarea = typeInto('  why is my book down?  ');
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    expect(emitted).toBe('why is my book down?');
    expect(textarea.value).toBe('');
  });

  it('should submit on Enter (without shift)', () => {
    let emitted: string | null = null;
    component.send.subscribe((v: string) => (emitted = v));
    const textarea = typeInto('hello');
    textarea.dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter'}));
    expect(emitted).toBe('hello');
  });

  it('should NOT submit on Shift+Enter', () => {
    let emitted: string | null = null;
    component.send.subscribe((v: string) => (emitted = v));
    const textarea = typeInto('line one');
    textarea.dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', shiftKey: true}));
    expect(emitted).toBeNull();
  });

  it('should not allow sending while loading', () => {
    fixture.componentRef.setInput('loading', true);
    typeInto('hi');
    expect(component.canSend()).toBe(false);
  });
});

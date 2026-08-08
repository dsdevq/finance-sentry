import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {ChatMessageComponent} from './chat-message.component';

describe('ChatMessageComponent', () => {
  let fixture: ComponentFixture<ChatMessageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [ChatMessageComponent]}).compileComponents();
    fixture = TestBed.createComponent(ChatMessageComponent);
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the message text', () => {
    fixture.componentRef.setInput('text', 'Your net worth is $1.8M.');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Your net worth is $1.8M.');
  });

  it('should align user messages to the end', () => {
    fixture.componentRef.setInput('role', 'user');
    fixture.detectChanges();
    const row = fixture.nativeElement.querySelector('div');
    expect(row.className).toContain('justify-end');
  });

  it('should align assistant messages to the start', () => {
    fixture.componentRef.setInput('role', 'assistant');
    fixture.detectChanges();
    const row = fixture.nativeElement.querySelector('div');
    expect(row.className).toContain('justify-start');
  });

  it('should render tool-progress chips', () => {
    fixture.componentRef.setInput('tools', [
      {name: 'get_portfolio_snapshot', running: true},
      {name: 'get_ips', running: false},
    ]);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('get_portfolio_snapshot');
    expect(text).toContain('get_ips');
  });

  it('should show a thinking indicator while streaming with no text yet', () => {
    fixture.componentRef.setInput('streaming', true);
    fixture.detectChanges();
    const pulse = fixture.nativeElement.querySelector('[aria-label="Thinking"]');
    expect(pulse).not.toBeNull();
  });
});

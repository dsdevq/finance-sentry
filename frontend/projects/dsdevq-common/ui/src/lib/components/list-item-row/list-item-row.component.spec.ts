import {ChangeDetectionStrategy, Component} from '@angular/core';
import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {ListItemRowComponent} from './list-item-row.component';

describe('ListItemRowComponent', () => {
  let fixture: ComponentFixture<ListItemRowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [ListItemRowComponent]}).compileComponents();
    fixture = TestBed.createComponent(ListItemRowComponent);
    fixture.componentRef.setInput('label', 'Netflix');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have display:block on the host element', () => {
    const host = fixture.nativeElement as HTMLElement;
    expect(host.style.display).toBe('block');
  });

  it('should render the label text', () => {
    const row: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(row.textContent).toContain('Netflix');
  });

  it('should not render a sublabel by default', () => {
    const labelWrapper: HTMLElement = fixture.nativeElement.querySelector('.flex-1');
    const divs: NodeListOf<HTMLElement> = labelWrapper.querySelectorAll('div');
    expect(divs.length).toBe(1);
  });

  it('should render the sublabel when provided', () => {
    fixture.componentRef.setInput('sublabel', 'Video streaming');
    fixture.detectChanges();
    const labelWrapper: HTMLElement = fixture.nativeElement.querySelector('.flex-1');
    const divs: NodeListOf<HTMLElement> = labelWrapper.querySelectorAll('div');
    expect(divs.length).toBe(2);
    expect(divs[1].textContent?.trim()).toBe('Video streaming');
  });

  it('should not apply opacity-55 by default', () => {
    const row: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(row.classList).not.toContain('opacity-55');
  });

  it('should apply opacity-55 when dimmed is true', () => {
    fixture.componentRef.setInput('dimmed', true);
    fixture.detectChanges();
    const row: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(row.classList).toContain('opacity-55');
  });

  it('should remove opacity-55 when dimmed changes back to false', () => {
    fixture.componentRef.setInput('dimmed', true);
    fixture.detectChanges();
    fixture.componentRef.setInput('dimmed', false);
    fixture.detectChanges();
    const row: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(row.classList).not.toContain('opacity-55');
  });

  it('should have the border and flex layout classes on the row div', () => {
    const row: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(row.classList).toContain('flex');
    expect(row.classList).toContain('items-center');
    expect(row.classList).toContain('border-b');
    expect(row.classList).toContain('border-border-default');
    expect(row.classList).toContain('px-cmn-4');
    expect(row.classList).toContain('py-cmn-3');
  });

  it('should have last:border-b-0 to suppress the border on the final row', () => {
    const row: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(row.className).toContain('last:border-b-0');
  });
});

@Component({
  imports: [ListItemRowComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <cmn-list-item-row label="Spotify">
      <div avatar data-testid="avatar">SP</div>
      <div meta data-testid="meta">Next: Jan 1</div>
      <div amount data-testid="amount">$9.99</div>
      <div actions data-testid="actions">
        <button>Dismiss</button>
      </div>
    </cmn-list-item-row>
  `,
})
class ListItemRowHostComponent {}

describe('ListItemRowComponent — content projection', () => {
  let fixture: ComponentFixture<ListItemRowHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListItemRowHostComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(ListItemRowHostComponent);
    fixture.detectChanges();
  });

  it('should project avatar slot content', () => {
    const avatar: HTMLElement | null =
      fixture.nativeElement.querySelector('[data-testid="avatar"]');
    expect(avatar).toBeTruthy();
    expect(avatar?.textContent?.trim()).toBe('SP');
  });

  it('should project meta slot content', () => {
    const meta: HTMLElement | null = fixture.nativeElement.querySelector('[data-testid="meta"]');
    expect(meta).toBeTruthy();
    expect(meta?.textContent?.trim()).toBe('Next: Jan 1');
  });

  it('should project amount slot content', () => {
    const amount: HTMLElement | null =
      fixture.nativeElement.querySelector('[data-testid="amount"]');
    expect(amount).toBeTruthy();
    expect(amount?.textContent?.trim()).toBe('$9.99');
  });

  it('should project actions slot content', () => {
    const actions: HTMLElement | null =
      fixture.nativeElement.querySelector('[data-testid="actions"]');
    expect(actions).toBeTruthy();
    const btn: HTMLButtonElement | null = actions?.querySelector('button') ?? null;
    expect(btn?.textContent?.trim()).toBe('Dismiss');
  });
});

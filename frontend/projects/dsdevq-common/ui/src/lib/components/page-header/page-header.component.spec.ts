/* eslint-disable @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access */
import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {By} from '@angular/platform-browser';
import {beforeEach, describe, expect, it} from 'vitest';

import {PageHeaderComponent} from './page-header.component';

describe('PageHeaderComponent', () => {
  let fixture: ComponentFixture<PageHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [PageHeaderComponent]}).compileComponents();
    fixture = TestBed.createComponent(PageHeaderComponent);
    fixture.componentRef.setInput('title', 'Accounts');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the title', () => {
    expect(fixture.nativeElement.textContent).toContain('Accounts');
  });

  it('should have display:block on the host element', () => {
    const host = fixture.nativeElement as HTMLElement;
    expect(host.style.display).toBe('block');
  });

  it('should not render subtitle when empty', () => {
    const p: HTMLElement | null = fixture.nativeElement.querySelector('p');
    expect(p).toBeNull();
  });

  it('should render subtitle when provided', () => {
    fixture.componentRef.setInput('subtitle', 'Manage your connected accounts');
    fixture.detectChanges();
    const p: HTMLElement | null = fixture.nativeElement.querySelector('p');
    expect(p?.textContent?.trim()).toBe('Manage your connected accounts');
  });

  it('should not render action button when actionLabel is null', () => {
    const btn = fixture.debugElement.query(By.css('cmn-button'));
    expect(btn).toBeNull();
  });

  it('should render action button when actionLabel is provided', () => {
    fixture.componentRef.setInput('actionLabel', 'Connect Account');
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('cmn-button'));
    expect(btn).toBeTruthy();
    expect(btn.nativeElement.textContent).toContain('Connect Account');
  });

  it('should emit actionClick when button is clicked', () => {
    fixture.componentRef.setInput('actionLabel', 'Connect Account');
    fixture.detectChanges();
    const emitted: void[] = [];
    fixture.componentInstance.actionClick.subscribe(() => emitted.push(undefined));
    const btn = fixture.debugElement.query(By.css('button'));
    btn.triggerEventHandler('click', new MouseEvent('click'));
    expect(emitted.length).toBe(1);
  });

  it('should pass loading state to the button', () => {
    fixture.componentRef.setInput('actionLabel', 'Connect Account');
    fixture.componentRef.setInput('actionLoading', true);
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('cmn-button'));
    expect(btn.componentInstance.loading()).toBe(true);
  });

  it('should pass disabled state to the button', () => {
    fixture.componentRef.setInput('actionLabel', 'Connect Account');
    fixture.componentRef.setInput('actionDisabled', true);
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('cmn-button'));
    expect(btn.componentInstance.disabled()).toBe(true);
  });
});

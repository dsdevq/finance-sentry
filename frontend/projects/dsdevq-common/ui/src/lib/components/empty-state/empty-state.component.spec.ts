import {ChangeDetectionStrategy, Component} from '@angular/core';
import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {EmptyStateComponent} from './empty-state.component';

describe('EmptyStateComponent — inputs', () => {
  let fixture: ComponentFixture<EmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [EmptyStateComponent]}).compileComponents();
    fixture = TestBed.createComponent(EmptyStateComponent);
    fixture.componentRef.setInput('message', 'Nothing here yet.');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the message text', () => {
    const p: HTMLElement | null = fixture.nativeElement.querySelector('p');
    expect(p?.textContent?.trim()).toBe('Nothing here yet.');
  });

  it('should not render a subMessage element when subMessage is null', () => {
    const paragraphs: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('p');
    expect(paragraphs.length).toBe(1);
  });

  it('should render a second paragraph when subMessage is provided', () => {
    fixture.componentRef.setInput('subMessage', 'Connect an account first.');
    fixture.detectChanges();
    const paragraphs: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('p');
    expect(paragraphs.length).toBe(2);
    expect(paragraphs[1].textContent?.trim()).toBe('Connect an account first.');
  });

  it('should not render an icon element when icon is null', () => {
    const icon = fixture.nativeElement.querySelector('cmn-icon, lucide-icon');
    expect(icon).toBeNull();
  });

  it('should render an icon element when icon input is provided', () => {
    fixture.componentRef.setInput('icon', 'CheckCircle2');
    fixture.detectChanges();
    const icon = fixture.nativeElement.querySelector('cmn-icon');
    expect(icon).toBeTruthy();
  });

  it('should apply iconClass to the icon element', () => {
    fixture.componentRef.setInput('icon', 'CheckCircle2');
    fixture.componentRef.setInput('iconClass', 'text-status-success');
    fixture.detectChanges();
    const icon: HTMLElement | null = fixture.nativeElement.querySelector('cmn-icon');
    expect(icon?.className).toContain('text-status-success');
  });

  it('should apply card wrapper classes for the card variant', () => {
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(wrapper.className).toContain('bg-surface-card');
    expect(wrapper.className).toContain('border-border-default');
  });

  it('should apply no wrapper classes for the bare variant', () => {
    fixture.componentRef.setInput('variant', 'bare');
    fixture.detectChanges();
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('div');
    expect(wrapper.className).not.toContain('bg-surface-card');
  });

  it('should render icon in card variant', () => {
    fixture.componentRef.setInput('icon', 'AlertCircle');
    fixture.detectChanges();
    const icon = fixture.nativeElement.querySelector('cmn-icon');
    expect(icon).toBeTruthy();
  });

  it('should render icon identically in bare variant', () => {
    fixture.componentRef.setInput('variant', 'bare');
    fixture.componentRef.setInput('icon', 'AlertCircle');
    fixture.detectChanges();
    const icon = fixture.nativeElement.querySelector('cmn-icon');
    expect(icon).toBeTruthy();
  });
});

@Component({
  imports: [EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <cmn-empty-state message="No items found">
      <button cta data-testid="cta-button" type="button">Connect Now</button>
    </cmn-empty-state>
  `,
})
class EmptyStateCtaHostComponent {}

describe('EmptyStateComponent — CTA slot projection', () => {
  let fixture: ComponentFixture<EmptyStateCtaHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyStateCtaHostComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(EmptyStateCtaHostComponent);
    fixture.detectChanges();
  });

  it('should project CTA slot content into the body', () => {
    const cta: HTMLElement | null = fixture.nativeElement.querySelector(
      '[data-testid="cta-button"]'
    );
    expect(cta).toBeTruthy();
    expect(cta?.textContent?.trim()).toBe('Connect Now');
  });

  it('should render CTA content inside the empty-state wrapper', () => {
    const emptyState: HTMLElement | null = fixture.nativeElement.querySelector('cmn-empty-state');
    const cta: HTMLElement | null = emptyState?.querySelector('[data-testid="cta-button"]') ?? null;
    expect(cta).toBeTruthy();
  });
});

@Component({
  imports: [EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <cmn-empty-state message="No items found" variant="bare">
      <button cta data-testid="bare-cta" type="button">Action</button>
    </cmn-empty-state>
  `,
})
class EmptyStateBareCtaHostComponent {}

describe('EmptyStateComponent — CTA slot projection in bare variant', () => {
  let fixture: ComponentFixture<EmptyStateBareCtaHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyStateBareCtaHostComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(EmptyStateBareCtaHostComponent);
    fixture.detectChanges();
  });

  it('should project CTA slot content in bare variant', () => {
    const cta: HTMLElement | null = fixture.nativeElement.querySelector('[data-testid="bare-cta"]');
    expect(cta).toBeTruthy();
    expect(cta?.textContent?.trim()).toBe('Action');
  });
});

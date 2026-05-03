import {ComponentFixture, TestBed} from '@angular/core/testing';

import {BadgeComponent, BadgeStatus} from './badge.component';

describe('BadgeComponent', () => {
  let fixture: ComponentFixture<BadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [BadgeComponent]}).compileComponents();
    fixture = TestBed.createComponent(BadgeComponent);
  });

  function indicator(): HTMLElement | null {
    return fixture.nativeElement.querySelector('.cmn-badge-indicator');
  }

  it('renders count when count > 0', () => {
    fixture.componentRef.setInput('count', 5);
    fixture.detectChanges();
    expect(indicator()?.textContent?.trim()).toBe('5');
  });

  it('hides indicator when count === 0 and showZero is false', () => {
    fixture.componentRef.setInput('count', 0);
    fixture.detectChanges();
    expect(indicator()).toBeNull();
  });

  it('renders 0 when showZero is true', () => {
    fixture.componentRef.setInput('count', 0);
    fixture.componentRef.setInput('showZero', true);
    fixture.detectChanges();
    expect(indicator()?.textContent?.trim()).toBe('0');
  });

  it('renders {overflowCount}+ when count > overflowCount (default 99)', () => {
    fixture.componentRef.setInput('count', 250);
    fixture.detectChanges();
    expect(indicator()?.textContent?.trim()).toBe('99+');
  });

  it('honors custom overflowCount', () => {
    fixture.componentRef.setInput('count', 12);
    fixture.componentRef.setInput('overflowCount', 9);
    fixture.detectChanges();
    expect(indicator()?.textContent?.trim()).toBe('9+');
  });

  it('dot mode renders an empty dot regardless of count', () => {
    fixture.componentRef.setInput('dot', true);
    fixture.componentRef.setInput('count', 7);
    fixture.detectChanges();
    expect(indicator()?.textContent?.trim()).toBe('');
    expect(indicator()?.className).toContain('w-2');
    expect(indicator()?.className).toContain('h-2');
  });

  it.each<[BadgeStatus, string]>([
    ['default', 'bg-neutral-500'],
    ['success', 'bg-status-success'],
    ['processing', 'bg-status-info'],
    ['error', 'bg-status-error'],
    ['warning', 'bg-status-warning'],
  ])('applies background class for status "%s"', (status, expectedClass) => {
    fixture.componentRef.setInput('count', 1);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
    expect(indicator()?.className).toContain(expectedClass);
  });

  it('uses inline (non-absolute) positioning when standalone is true', () => {
    fixture.componentRef.setInput('count', 3);
    fixture.componentRef.setInput('standalone', true);
    fixture.detectChanges();
    expect(indicator()?.className).not.toContain('absolute');
  });

  it('uses absolute positioning by default', () => {
    fixture.componentRef.setInput('count', 3);
    fixture.detectChanges();
    expect(indicator()?.className).toContain('absolute');
  });
});

import {ComponentFixture, TestBed} from '@angular/core/testing';

import {BadgeComponent, BadgeVariant} from './badge.component';

describe('BadgeComponent', () => {
  let fixture: ComponentFixture<BadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [BadgeComponent]}).compileComponents();
    fixture = TestBed.createComponent(BadgeComponent);
    fixture.componentRef.setInput('variant', 'neutral' as BadgeVariant);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should apply success classes for success variant', () => {
    fixture.componentRef.setInput('variant', 'success' as BadgeVariant);
    fixture.detectChanges();
    const span: HTMLSpanElement = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('text-status-success');
  });

  it('should apply error classes for error variant', () => {
    fixture.componentRef.setInput('variant', 'error' as BadgeVariant);
    fixture.detectChanges();
    const span: HTMLSpanElement = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('text-status-error');
  });

  it('should apply neutral classes by default', () => {
    const span: HTMLSpanElement = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('text-text-secondary');
  });
});

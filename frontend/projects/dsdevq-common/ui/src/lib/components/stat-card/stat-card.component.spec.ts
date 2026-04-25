import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {StatCardComponent} from './stat-card.component';

describe('StatCardComponent', () => {
  let fixture: ComponentFixture<StatCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [StatCardComponent]}).compileComponents();
    fixture = TestBed.createComponent(StatCardComponent);
    fixture.componentRef.setInput('label', 'Total Wealth');
    fixture.componentRef.setInput('value', '$1,420,892.12');
    fixture.componentRef.setInput('delta', null);
    fixture.componentRef.setInput('deltaLabel', '');
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display label and value', () => {
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Total Wealth');
    expect(text).toContain('$1,420,892.12');
  });

  it('should show loading skeleton when loading is true', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const skeleton: HTMLElement | null = fixture.nativeElement.querySelector('.animate-pulse');
    expect(skeleton).toBeTruthy();
  });

  it('should apply success delta class for positive delta', () => {
    fixture.componentRef.setInput('delta', 4.2);
    fixture.componentRef.setInput('deltaLabel', '+4.2%');
    fixture.detectChanges();
    const deltaSpan: HTMLElement | null =
      fixture.nativeElement.querySelector('.text-status-success');
    expect(deltaSpan).toBeTruthy();
  });

  it('should apply error delta class for negative delta', () => {
    fixture.componentRef.setInput('delta', -2.1);
    fixture.componentRef.setInput('deltaLabel', '-2.1%');
    fixture.detectChanges();
    const deltaSpan: HTMLElement | null = fixture.nativeElement.querySelector('.text-status-error');
    expect(deltaSpan).toBeTruthy();
  });
});

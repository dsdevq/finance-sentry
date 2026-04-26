import {ComponentFixture, TestBed} from '@angular/core/testing';

import {InstitutionAvatarComponent} from './institution-avatar.component';

describe('InstitutionAvatarComponent', () => {
  let fixture: ComponentFixture<InstitutionAvatarComponent>;

  const setup = (name: string, size?: 'sm' | 'md' | 'lg'): HTMLSpanElement => {
    fixture = TestBed.createComponent(InstitutionAvatarComponent);
    fixture.componentRef.setInput('name', name);
    if (size) {
      fixture.componentRef.setInput('size', size);
    }
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('span');
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [InstitutionAvatarComponent]}).compileComponents();
  });

  it('derives two-letter initials from a multi-word name', () => {
    const span = setup('Wells Fargo');
    expect(span.textContent?.trim()).toBe('WF');
  });

  it('derives the first two letters of a single-word name', () => {
    const span = setup('Coinbase');
    expect(span.textContent?.trim()).toBe('CO');
  });

  it('uppercases initials regardless of input case', () => {
    const span = setup('chase bank');
    expect(span.textContent?.trim()).toBe('CB');
  });

  it('falls back to empty string when given whitespace only', () => {
    const span = setup('   ');
    expect(span.textContent?.trim()).toBe('');
  });

  it('uses only the first two words when given more', () => {
    const span = setup('Bank of America');
    expect(span.textContent?.trim()).toBe('BO');
  });

  it('exposes the institution name via aria-label', () => {
    const span = setup('Vanguard');
    expect(span.getAttribute('aria-label')).toBe('Vanguard');
  });

  it('applies the md size by default', () => {
    const span = setup('Fidelity');
    expect(span.className).toContain('h-7');
    expect(span.className).toContain('w-7');
  });

  it('applies sm size classes when size is sm', () => {
    const span = setup('Kraken', 'sm');
    expect(span.className).toContain('h-6');
    expect(span.className).toContain('w-6');
  });

  it('applies lg size classes when size is lg', () => {
    const span = setup('Kraken', 'lg');
    expect(span.className).toContain('h-9');
    expect(span.className).toContain('w-9');
  });
});

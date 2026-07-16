import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {PageContainerComponent} from './page-container.component';

describe('PageContainerComponent', () => {
  let fixture: ComponentFixture<PageContainerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [PageContainerComponent]}).compileComponents();
    fixture = TestBed.createComponent(PageContainerComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have display:block on the host element', () => {
    const host = fixture.nativeElement as HTMLElement;
    expect(host.style.display).toBe('block');
  });

  it('should render the outer padding container', () => {
    const outer: HTMLElement | null = fixture.nativeElement.querySelector('div');
    expect(outer?.classList).toContain('p-cmn-6');
  });

  it('should apply default max-width and spacing to the inner container', () => {
    const inner: HTMLElement | null = fixture.nativeElement.querySelector('div > div');
    expect(inner?.classList).toContain('mx-auto');
    expect(inner?.classList).toContain('max-w-screen-lg');
    expect(inner?.classList).toContain('space-y-cmn-5');
  });

  it('should apply a custom maxWidth class to the inner container', () => {
    fixture.componentRef.setInput('maxWidth', 'max-w-[900px]');
    fixture.detectChanges();
    const inner: HTMLElement | null = fixture.nativeElement.querySelector('div > div');
    expect(inner?.classList).toContain('max-w-[900px]');
    expect(inner?.classList).not.toContain('max-w-screen-lg');
  });

  it('should apply lg spacing class when spacing is "lg"', () => {
    fixture.componentRef.setInput('spacing', 'lg');
    fixture.detectChanges();
    const inner: HTMLElement | null = fixture.nativeElement.querySelector('div > div');
    expect(inner?.classList).toContain('space-y-cmn-10');
    expect(inner?.classList).not.toContain('space-y-cmn-5');
  });

  it('should apply md spacing class when spacing is "md"', () => {
    fixture.componentRef.setInput('spacing', 'md');
    fixture.detectChanges();
    const inner: HTMLElement | null = fixture.nativeElement.querySelector('div > div');
    expect(inner?.classList).toContain('space-y-cmn-5');
  });
});

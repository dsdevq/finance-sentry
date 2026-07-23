import {
  ChangeDetectionStrategy,
  Component,
  type ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {SectionHeaderComponent} from './section-header.component';

@Component({
  template:
    '<cmn-section-header label="Test"><span id="projected">slot</span></cmn-section-header>',
  imports: [SectionHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class ProjectionHostComponent {}

describe('SectionHeaderComponent', () => {
  describe('inputs and host layout', () => {
    let fixture: ComponentFixture<SectionHeaderComponent>;

    beforeEach(async () => {
      await TestBed.configureTestingModule({imports: [SectionHeaderComponent]}).compileComponents();
      fixture = TestBed.createComponent(SectionHeaderComponent);
    });

    it('renders the label as an h2 by default', () => {
      fixture.componentRef.setInput('label', 'Accounts');
      fixture.detectChanges();
      const h2 = fixture.nativeElement.querySelector<HTMLHeadingElement>('h2');
      expect(h2?.textContent?.trim()).toBe('Accounts');
    });

    it('renders a span when labelAs is "span"', () => {
      fixture.componentRef.setInput('label', 'Filter');
      fixture.componentRef.setInput('labelAs', 'span');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('h2')).toBeNull();
      const span = fixture.nativeElement.querySelector<HTMLSpanElement>('span');
      expect(span?.textContent?.trim()).toBe('Filter');
    });

    it('applies justify-between to the host by default', () => {
      fixture.componentRef.setInput('label', 'X');
      fixture.detectChanges();
      expect(fixture.nativeElement.className).toContain('justify-between');
      expect(fixture.nativeElement.className).not.toContain('gap-cmn-2');
    });

    it('applies gap-cmn-2 to the host when align is "start"', () => {
      fixture.componentRef.setInput('label', 'X');
      fixture.componentRef.setInput('align', 'start');
      fixture.detectChanges();
      expect(fixture.nativeElement.className).toContain('gap-cmn-2');
      expect(fixture.nativeElement.className).not.toContain('justify-between');
    });
  });

  describe('content projection', () => {
    let hostFixture: ComponentFixture<ProjectionHostComponent>;

    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [ProjectionHostComponent],
      }).compileComponents();
      hostFixture = TestBed.createComponent(ProjectionHostComponent);
      hostFixture.detectChanges();
    });

    it('projects slotted content into the layout', () => {
      const projected = hostFixture.nativeElement.querySelector<HTMLSpanElement>('#projected');
      expect(projected).not.toBeNull();
      expect(projected?.textContent?.trim()).toBe('slot');
    });
  });
});

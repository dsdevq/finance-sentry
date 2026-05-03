import {ComponentFixture, TestBed} from '@angular/core/testing';

import {TagComponent, TagVariant} from './tag.component';

describe('TagComponent', () => {
  let fixture: ComponentFixture<TagComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [TagComponent]}).compileComponents();
    fixture = TestBed.createComponent(TagComponent);
    fixture.componentRef.setInput('variant', 'neutral' as TagVariant);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should apply success classes for success variant', () => {
    fixture.componentRef.setInput('variant', 'success' as TagVariant);
    fixture.detectChanges();
    const span: HTMLSpanElement = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('text-status-success');
  });

  it('should apply error classes for error variant', () => {
    fixture.componentRef.setInput('variant', 'error' as TagVariant);
    fixture.detectChanges();
    const span: HTMLSpanElement = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('text-status-error');
  });

  it('should apply neutral classes by default', () => {
    const span: HTMLSpanElement = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('text-text-secondary');
  });
});

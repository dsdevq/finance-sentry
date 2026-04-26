import {ChangeDetectionStrategy, Component, signal} from '@angular/core';
import {ComponentFixture, TestBed} from '@angular/core/testing';

import {StatusIndicatorComponent, type StatusIndicatorVariant} from './status-indicator.component';

@Component({
  imports: [StatusIndicatorComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <cmn-status-indicator [variant]="variant()" [timestampLabel]="timestamp()">
      {{ label() }}
    </cmn-status-indicator>
  `,
})
class HostComponent {
  public readonly variant = signal<StatusIndicatorVariant>('neutral');
  public readonly timestamp = signal<string | null>(null);
  public readonly label = signal('Synced');
}

describe('StatusIndicatorComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [HostComponent]}).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  const querySpans = (): HTMLSpanElement[] =>
    Array.from(fixture.nativeElement.querySelectorAll('span'));

  it('projects label content into the status row', () => {
    host.label.set('Stale');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Stale');
  });

  it('applies success variant classes to text and dot', () => {
    host.variant.set('success');
    fixture.detectChanges();
    const [statusSpan, dotSpan] = querySpans();
    expect(statusSpan.className).toContain('text-status-success');
    expect(dotSpan.className).toContain('bg-status-success');
  });

  it('applies warning variant classes to text and dot', () => {
    host.variant.set('warning');
    fixture.detectChanges();
    const [statusSpan, dotSpan] = querySpans();
    expect(statusSpan.className).toContain('text-status-warning');
    expect(dotSpan.className).toContain('bg-status-warning');
  });

  it('applies error variant classes to text and dot', () => {
    host.variant.set('error');
    fixture.detectChanges();
    const [statusSpan, dotSpan] = querySpans();
    expect(statusSpan.className).toContain('text-status-error');
    expect(dotSpan.className).toContain('bg-status-error');
  });

  it('does not render timestamp line when label is null', () => {
    host.timestamp.set(null);
    fixture.detectChanges();
    expect(querySpans()).toHaveLength(2);
  });

  it('renders timestamp line when label is provided', () => {
    host.timestamp.set('3 mins ago');
    fixture.detectChanges();
    const spans = querySpans();
    expect(spans).toHaveLength(3);
    expect(spans[2].textContent?.trim()).toBe('3 mins ago');
  });

  it('hides timestamp line when label is empty string', () => {
    host.timestamp.set('');
    fixture.detectChanges();
    expect(querySpans()).toHaveLength(2);
  });
});

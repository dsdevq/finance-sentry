import {ChangeDetectionStrategy, Component, signal} from '@angular/core';
import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {type CmnTab, TabGroupComponent} from './tab-group.component';

const SAMPLE_TABS: CmnTab[] = [
  {id: 'overview', label: 'Overview'},
  {id: 'details', label: 'Details'},
  {id: 'history', label: 'History'},
];

describe('TabGroupComponent', () => {
  let fixture: ComponentFixture<TabGroupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [TabGroupComponent]}).compileComponents();
    fixture = TestBed.createComponent(TabGroupComponent);
    fixture.componentRef.setInput('tabs', SAMPLE_TABS);
    fixture.componentRef.setInput('activeTab', 'overview');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have display:block on the host element', () => {
    const host = fixture.nativeElement as HTMLElement;
    expect(host.style.display).toBe('block');
  });

  it('should render a button for each tab', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(buttons.length).toBe(SAMPLE_TABS.length);
    expect(buttons[0].textContent?.trim()).toBe('Overview');
    expect(buttons[1].textContent?.trim()).toBe('Details');
    expect(buttons[2].textContent?.trim()).toBe('History');
  });

  it('should render the tablist container with role="tablist"', () => {
    const tablist: HTMLElement | null = fixture.nativeElement.querySelector('[role="tablist"]');
    expect(tablist).toBeTruthy();
  });

  it('should apply active classes to the selected tab', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(buttons[0].classList).toContain('border-accent-default');
    expect(buttons[0].classList).toContain('text-text-primary');
  });

  it('should apply inactive classes to non-selected tabs', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(buttons[1].classList).toContain('border-transparent');
    expect(buttons[1].classList).toContain('text-text-secondary');
    expect(buttons[2].classList).toContain('border-transparent');
    expect(buttons[2].classList).toContain('text-text-secondary');
  });

  it('should set aria-selected="true" on the active tab', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(buttons[0].getAttribute('aria-selected')).toBe('true');
  });

  it('should set aria-selected="false" on inactive tabs', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(buttons[1].getAttribute('aria-selected')).toBe('false');
    expect(buttons[2].getAttribute('aria-selected')).toBe('false');
  });

  it('should update the active tab signal when a tab button is clicked', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    buttons[1].click();
    fixture.detectChanges();
    expect(fixture.componentInstance.activeTab()).toBe('details');
  });

  it('should swap active/inactive classes after a tab click', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    buttons[1].click();
    fixture.detectChanges();
    expect(buttons[1].classList).toContain('border-accent-default');
    expect(buttons[0].classList).toContain('border-transparent');
  });

  it('should not apply active class to any tab when activeTab is empty', () => {
    fixture.componentRef.setInput('activeTab', '');
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    buttons.forEach(btn => {
      expect(btn.classList).not.toContain('border-accent-default');
    });
  });

  it('should apply shared base classes to every tab button', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    buttons.forEach(btn => {
      expect(btn.classList).toContain('px-cmn-4');
      expect(btn.classList).toContain('py-cmn-2');
      expect(btn.classList).toContain('text-cmn-sm');
      expect(btn.classList).toContain('font-medium');
      expect(btn.classList).toContain('border-b-2');
    });
  });

  it('should re-render when tabs input changes', () => {
    fixture.componentRef.setInput('tabs', [{id: 'a', label: 'Alpha'}]);
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(buttons.length).toBe(1);
    expect(buttons[0].textContent?.trim()).toBe('Alpha');
  });
});

@Component({
  imports: [TabGroupComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<cmn-tab-group [tabs]="tabs" [(activeTab)]="current" />',
})
class TwoWayHostComponent {
  public tabs: CmnTab[] = SAMPLE_TABS;
  // A signal, not a plain property: under zoneless change detection a mutated plain
  // property never marks the OnPush host dirty, so the view would not re-render.
  public readonly current = signal('overview');
}

describe('TabGroupComponent — two-way model binding', () => {
  let fixture: ComponentFixture<TwoWayHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [TwoWayHostComponent]}).compileComponents();
    fixture = TestBed.createComponent(TwoWayHostComponent);
    fixture.detectChanges();
  });

  it('should sync the host property when a tab is clicked', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    buttons[2].click();
    fixture.detectChanges();
    expect(fixture.componentInstance.current()).toBe('history');
  });

  it('should reflect host property change in the rendered active tab', () => {
    fixture.componentInstance.current.set('details');
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(buttons[1].classList).toContain('border-accent-default');
    expect(buttons[0].classList).toContain('border-transparent');
  });
});

@Component({
  imports: [TabGroupComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <cmn-tab-group [tabs]="tabs" activeTab="overview">
      <p data-testid="panel">Panel content</p>
    </cmn-tab-group>
  `,
})
class ContentProjectionHostComponent {
  public readonly tabs: CmnTab[] = SAMPLE_TABS;
}

describe('TabGroupComponent — content projection', () => {
  let fixture: ComponentFixture<ContentProjectionHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContentProjectionHostComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(ContentProjectionHostComponent);
    fixture.detectChanges();
  });

  it('should project default slot content below the tab bar', () => {
    const panel: HTMLElement | null = fixture.nativeElement.querySelector('[data-testid="panel"]');
    expect(panel).toBeTruthy();
    expect(panel?.textContent?.trim()).toBe('Panel content');
  });

  it('should render the tab bar above the projected content', () => {
    const host: HTMLElement = fixture.nativeElement.querySelector('cmn-tab-group');
    const allChildren = Array.from(host.children);
    const tablistIdx = allChildren.findIndex(el => el.getAttribute('role') === 'tablist');
    // The projected <p> is itself the child element, so match it directly —
    // querySelector alone only searches descendants and would miss it.
    const panelIdx = allChildren.findIndex(
      el =>
        el.matches('[data-testid="panel"]') || el.querySelector('[data-testid="panel"]') !== null
    );
    expect(tablistIdx).toBeGreaterThanOrEqual(0);
    expect(panelIdx).toBeGreaterThanOrEqual(0);
    expect(tablistIdx).toBeLessThan(panelIdx);
  });
});

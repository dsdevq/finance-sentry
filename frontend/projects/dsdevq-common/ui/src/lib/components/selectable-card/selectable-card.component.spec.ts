import {ComponentFixture, TestBed} from '@angular/core/testing';

import {SelectableCardComponent, type SelectableCardOrientation} from './selectable-card.component';

describe('SelectableCardComponent', () => {
  let fixture: ComponentFixture<SelectableCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [SelectableCardComponent]}).compileComponents();
    fixture = TestBed.createComponent(SelectableCardComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('applies horizontal orientation classes by default', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.className).toContain('flex-row');
  });

  it('applies vertical orientation classes when set', () => {
    fixture.componentRef.setInput('orientation', 'vertical' as SelectableCardOrientation);
    fixture.detectChanges();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.className).toContain('flex-col');
  });

  it('reflects selected state via aria-pressed and accent border', () => {
    fixture.componentRef.setInput('selected', true);
    fixture.detectChanges();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-pressed')).toBe('true');
    expect(button.className).toContain('border-accent-default');
  });

  it('disables interaction when disabled is true', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(true);
  });

  it('emits clicked output on button click', () => {
    let count = 0;
    fixture.componentInstance.clicked.subscribe(() => count++);
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    expect(count).toBe(1);
  });
});

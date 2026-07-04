import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {ChipComponent} from './chip.component';

describe('ChipComponent', () => {
  let fixture: ComponentFixture<ChipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChipComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ChipComponent);
  });

  it('reflects the selected state via aria-pressed', () => {
    fixture.componentRef.setInput('selected', true);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    const button = element.querySelector('button');
    expect(button?.getAttribute('aria-pressed')).toBe('true');
  });

  it('emits clicked when the button is pressed', () => {
    let count = 0;
    fixture.componentInstance.clicked.subscribe(() => count++);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    const button = element.querySelector<HTMLButtonElement>('button');
    button?.click();
    expect(count).toBe(1);
  });
});

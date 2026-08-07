import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {BarChartComponent, type BarSeries} from './bar-chart.component';

const SAMPLE: BarSeries[] = [
  {
    label: 'Income',
    points: [
      {label: 'Jan', value: 3000},
      {label: 'Feb', value: 3200},
    ],
  },
  {
    label: 'Spending',
    points: [
      {label: 'Jan', value: 2100},
      {label: 'Feb', value: 2400},
    ],
  },
];

describe('BarChartComponent', () => {
  let fixture: ComponentFixture<BarChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [BarChartComponent]}).compileComponents();
    fixture = TestBed.createComponent(BarChartComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render a canvas element', () => {
    expect(fixture.nativeElement.querySelector('canvas')).toBeTruthy();
  });

  it('should display the label', () => {
    fixture.componentRef.setInput('label', 'Income vs Spending');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Income vs Spending');
  });

  it('should accept grouped series without throwing', () => {
    expect(() => {
      fixture.componentRef.setInput('series', SAMPLE);
      fixture.detectChanges();
    }).not.toThrow();
  });

  it('should accept percent value format without throwing', () => {
    expect(() => {
      fixture.componentRef.setInput('valueFormat', 'percent');
      fixture.componentRef.setInput('series', [
        {label: 'Savings rate', points: [{label: 'Jan', value: 30}]},
      ]);
      fixture.detectChanges();
    }).not.toThrow();
  });
});

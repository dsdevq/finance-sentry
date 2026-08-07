import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {AreaChartComponent, type AreaSeries} from './area-chart.component';

const SAMPLE: AreaSeries[] = [
  {
    label: 'Banking',
    points: [
      {label: 'Jan', value: 100},
      {label: 'Feb', value: 120},
    ],
  },
  {
    label: 'Crypto',
    points: [
      {label: 'Jan', value: 40},
      {label: 'Feb', value: 55},
    ],
  },
];

describe('AreaChartComponent', () => {
  let fixture: ComponentFixture<AreaChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [AreaChartComponent]}).compileComponents();
    fixture = TestBed.createComponent(AreaChartComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render a canvas element', () => {
    expect(fixture.nativeElement.querySelector('canvas')).toBeTruthy();
  });

  it('should display the label', () => {
    fixture.componentRef.setInput('label', 'Net Worth Composition');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Net Worth Composition');
  });

  it('should accept multi-series input without throwing', () => {
    expect(() => {
      fixture.componentRef.setInput('series', SAMPLE);
      fixture.detectChanges();
    }).not.toThrow();
  });
});

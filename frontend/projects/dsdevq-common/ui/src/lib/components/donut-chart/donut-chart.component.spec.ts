import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {DonutChartComponent} from './donut-chart.component';

describe('DonutChartComponent', () => {
  let fixture: ComponentFixture<DonutChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [DonutChartComponent]}).compileComponents();
    fixture = TestBed.createComponent(DonutChartComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render a canvas element', () => {
    const canvas = fixture.nativeElement.querySelector('canvas');
    expect(canvas).toBeTruthy();
  });

  it('should display the label', () => {
    fixture.componentRef.setInput('label', 'Allocation');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Allocation');
  });
});

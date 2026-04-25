import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {LineChartComponent} from './line-chart.component';

describe('LineChartComponent', () => {
  let fixture: ComponentFixture<LineChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [LineChartComponent]}).compileComponents();
    fixture = TestBed.createComponent(LineChartComponent);
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
    fixture.componentRef.setInput('label', 'Net Worth Performance');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Net Worth Performance');
  });
});

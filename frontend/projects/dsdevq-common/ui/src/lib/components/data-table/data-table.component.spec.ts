import {ChangeDetectionStrategy, Component, input} from '@angular/core';
import {ComponentFixture, TestBed} from '@angular/core/testing';

import {CmnCellDirective, CmnHeaderCellDirective} from './data-table-cell.directive';
import {CmnColumnComponent} from './data-table-column.component';
import {DataTableComponent} from './data-table.component';

interface Row {
  name: string;
  amount: number;
}

@Component({
  selector: 'test-host',
  imports: [
    CmnCellDirective,
    CmnColumnComponent,
    CmnHeaderCellDirective,
    DataTableComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <cmn-data-table [rows]="rows()" [emptyMessage]="emptyMessage()">
      <cmn-column key="name" header="Name">
        <ng-template cmnCell let-row>{{ row.name }}</ng-template>
      </cmn-column>
      <cmn-column key="amount" align="right">
        <ng-template cmnHeaderCell>Amount</ng-template>
        <ng-template cmnCell let-row>\${{ row.amount }}</ng-template>
      </cmn-column>
    </cmn-data-table>
  `,
})
class TestHostComponent {
  public readonly rows = input<Row[]>([]);
  public readonly emptyMessage = input<string>('No data');
}

const ROWS: Row[] = [
  {name: 'Groceries', amount: 54},
  {name: 'Salary', amount: 5000},
];

describe('DataTableComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should show empty message when rows is empty', () => {
    fixture.componentRef.setInput('emptyMessage', 'Nothing here');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nothing here');
  });

  it('should render column headers (string and template)', () => {
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Name');
    expect(text).toContain('Amount');
  });

  it('should render row data via projected cell templates', () => {
    fixture.componentRef.setInput('rows', ROWS);
    fixture.detectChanges();
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Groceries');
    expect(text).toContain('$5000');
  });
});

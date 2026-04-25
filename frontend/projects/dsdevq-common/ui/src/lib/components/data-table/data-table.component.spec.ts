import {ComponentFixture, TestBed} from '@angular/core/testing';

import type {TableColumn} from './data-table.component';
import {DataTableComponent} from './data-table.component';

interface Row {
  name: string;
  amount: number;
}

const COLUMNS: TableColumn<Row>[] = [
  {key: 'name', header: 'Name', cell: r => r.name},
  {key: 'amount', header: 'Amount', align: 'right', cell: r => `$${r.amount}`},
];

const ROWS: Row[] = [
  {name: 'Groceries', amount: 54},
  {name: 'Salary', amount: 5000},
];

describe('DataTableComponent', () => {
  let fixture: ComponentFixture<DataTableComponent<Row>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DataTableComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(DataTableComponent<Row>);
    fixture.componentRef.setInput('columns', COLUMNS);
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

  it('should render column headers', () => {
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Name');
    expect(text).toContain('Amount');
  });

  it('should render row data', () => {
    fixture.componentRef.setInput('rows', ROWS);
    fixture.detectChanges();
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Groceries');
    expect(text).toContain('$5000');
  });
});

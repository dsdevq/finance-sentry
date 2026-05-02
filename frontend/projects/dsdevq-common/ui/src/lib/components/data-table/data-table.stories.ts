import type {Meta, StoryObj} from '@storybook/angular';
import {moduleMetadata} from '@storybook/angular';

import {CmnCellDirective, CmnHeaderCellDirective} from './data-table-cell.directive';
import {CmnColumnComponent} from './data-table-column.component';
import {DataTableComponent} from './data-table.component';

interface Transaction {
  date: string;
  description: string;
  account: string;
  amount: string;
  status: string;
}

const ROWS: Transaction[] = [
  {date: 'Apr 24', description: 'Netflix', account: 'Chase Checking', amount: '-$15.99', status: 'Settled'},
  {date: 'Apr 23', description: 'Salary', account: 'Chase Checking', amount: '+$5,000.00', status: 'Settled'},
  {date: 'Apr 22', description: 'Whole Foods', account: 'Chase Checking', amount: '-$87.43', status: 'Pending'},
  {date: 'Apr 21', description: 'Dividends', account: 'IBKR', amount: '+$124.50', status: 'Settled'},
  {date: 'Apr 20', description: 'BTC Purchase', account: 'Binance', amount: '-$500.00', status: 'Settled'},
];

const meta: Meta<DataTableComponent<Transaction>> = {
  title: 'Components/DataTable',
  component: DataTableComponent,
  tags: ['autodocs'],
  decorators: [
    moduleMetadata({
      imports: [CmnCellDirective, CmnColumnComponent, CmnHeaderCellDirective],
    }),
  ],
};

export default meta;
type Story = StoryObj<DataTableComponent<Transaction>>;

const TEMPLATE = `
  <cmn-data-table [rows]="rows" [emptyMessage]="emptyMessage">
    <cmn-column key="date" header="Date">
      <ng-template cmnCell let-row>{{ row.date }}</ng-template>
    </cmn-column>
    <cmn-column key="description" header="Description">
      <ng-template cmnCell let-row>{{ row.description }}</ng-template>
    </cmn-column>
    <cmn-column key="account" header="Account">
      <ng-template cmnCell let-row>{{ row.account }}</ng-template>
    </cmn-column>
    <cmn-column key="amount" header="Amount" align="right">
      <ng-template cmnCell let-row>{{ row.amount }}</ng-template>
    </cmn-column>
    <cmn-column key="status" header="Status" align="center">
      <ng-template cmnCell let-row>{{ row.status }}</ng-template>
    </cmn-column>
  </cmn-data-table>
`;

export const Default: Story = {
  args: {rows: ROWS},
  render: args => ({props: args, template: TEMPLATE}),
};

export const Empty: Story = {
  args: {rows: [], emptyMessage: 'No recent transactions'},
  render: args => ({props: args, template: TEMPLATE}),
};

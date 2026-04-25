import type {Meta, StoryObj} from '@storybook/angular';

import type {TableColumn} from './data-table.component';
import {DataTableComponent} from './data-table.component';

interface Transaction {
  date: string;
  description: string;
  account: string;
  amount: string;
  status: string;
}

const COLUMNS: TableColumn<Transaction>[] = [
  {key: 'date', header: 'Date', cell: r => r.date},
  {key: 'description', header: 'Description', cell: r => r.description},
  {key: 'account', header: 'Account', cell: r => r.account},
  {key: 'amount', header: 'Amount', align: 'right', cell: r => r.amount},
  {key: 'status', header: 'Status', align: 'center', cell: r => r.status},
];

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
};

export default meta;
type Story = StoryObj<DataTableComponent<Transaction>>;

export const Default: Story = {
  args: {columns: COLUMNS, rows: ROWS},
};

export const Empty: Story = {
  args: {columns: COLUMNS, rows: [], emptyMessage: 'No recent transactions'},
};

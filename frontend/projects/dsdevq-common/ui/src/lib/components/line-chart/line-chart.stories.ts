import type {Meta, StoryObj} from '@storybook/angular';

import type {ChartPoint} from './line-chart.component';
import {LineChartComponent} from './line-chart.component';

const SAMPLE_DATA: ChartPoint[] = [
  {label: 'May', value: 1280000},
  {label: 'Jun', value: 1310000},
  {label: 'Jul', value: 1295000},
  {label: 'Aug', value: 1340000},
  {label: 'Sep', value: 1360000},
  {label: 'Oct', value: 1380000},
  {label: 'Nov', value: 1355000},
  {label: 'Dec', value: 1390000},
  {label: 'Jan', value: 1400000},
  {label: 'Feb', value: 1395000},
  {label: 'Mar', value: 1410000},
  {label: 'Apr', value: 1420892},
];

const meta: Meta<LineChartComponent> = {
  title: 'Components/LineChart',
  component: LineChartComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<LineChartComponent>;

export const Default: Story = {
  args: {data: SAMPLE_DATA, label: 'Net Worth Performance', currency: 'USD'},
};

export const Empty: Story = {
  args: {data: [], label: 'Net Worth Performance', currency: 'USD'},
};

import type {Meta, StoryObj} from '@storybook/angular';

import type {DonutSegment} from './donut-chart.component';
import {DonutChartComponent} from './donut-chart.component';

const SAMPLE_SEGMENTS: DonutSegment[] = [
  {label: 'Banks', value: 412050},
  {label: 'Brokerage', value: 924521},
  {label: 'Crypto', value: 84320},
];

const meta: Meta<DonutChartComponent> = {
  title: 'Components/DonutChart',
  component: DonutChartComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<DonutChartComponent>;

export const Default: Story = {
  args: {segments: SAMPLE_SEGMENTS, label: 'Allocation', currency: 'USD'},
};

export const Empty: Story = {
  args: {segments: [], label: 'Allocation', currency: 'USD'},
};

export const CustomColors: Story = {
  args: {
    segments: [
      {label: 'Banks', value: 412050, color: '#4f46e5'},
      {label: 'Brokerage', value: 924521, color: '#10b981'},
      {label: 'Crypto', value: 84320, color: '#f59e0b'},
    ],
    label: 'Allocation',
    currency: 'USD',
  },
};

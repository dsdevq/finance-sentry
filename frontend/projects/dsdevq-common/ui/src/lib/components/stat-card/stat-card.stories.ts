import type {Meta, StoryObj} from '@storybook/angular';

import {StatCardComponent} from './stat-card.component';

const meta: Meta<StatCardComponent> = {
  title: 'Components/StatCard',
  component: StatCardComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<StatCardComponent>;

export const Default: Story = {
  args: {label: 'Total Wealth', value: '$1,420,892.12', delta: null},
};

export const WithPositiveDelta: Story = {
  args: {label: 'Banks', value: '$412,050.00', delta: 1.8, deltaLabel: '+1.8% this month'},
};

export const WithNegativeDelta: Story = {
  args: {label: 'Crypto', value: '$84,320.45', delta: -2.1, deltaLabel: '-2.1% this month'},
};

export const WithIcon: Story = {
  args: {label: 'Brokerage', value: '$924,521.67', delta: 1.8, deltaLabel: '+1.8%', icon: 'TrendingUp'},
};

export const Loading: Story = {
  args: {label: 'Total Wealth', value: '', loading: true},
};

export const AllCards: Story = {
  render: () => ({
    template: `
      <div class="grid grid-cols-4 gap-4 p-4 bg-surface-bg">
        <cmn-stat-card label="Total Wealth" value="$1,420,892.12" [delta]="4.2" deltaLabel="+4.2% this month" icon="Wallet" />
        <cmn-stat-card label="Banks" value="$412,050.00" [delta]="1.8" deltaLabel="+1.8%" icon="Building2" />
        <cmn-stat-card label="Crypto" value="$84,320.45" [delta]="-2.1" deltaLabel="-2.1%" icon="Bitcoin" />
        <cmn-stat-card label="Brokerage" value="$924,521.67" [delta]="1.8" deltaLabel="+1.8%" icon="BarChart2" />
      </div>
    `,
  }),
};

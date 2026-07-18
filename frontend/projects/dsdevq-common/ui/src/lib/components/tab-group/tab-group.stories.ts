import type {Meta, StoryObj} from '@storybook/angular';

import {TabGroupComponent} from './tab-group.component';

const meta: Meta<TabGroupComponent> = {
  title: 'Components/TabGroup',
  component: TabGroupComponent,
  tags: ['autodocs'],
  argTypes: {
    activeTab: {control: 'text'},
  },
};

export default meta;
type Story = StoryObj<TabGroupComponent>;

export const Default: Story = {
  args: {
    tabs: [
      {id: 'overview', label: 'Overview'},
      {id: 'details', label: 'Details'},
      {id: 'history', label: 'History'},
    ],
    activeTab: 'overview',
  },
};

export const SecondTabActive: Story = {
  args: {
    tabs: [
      {id: 'overview', label: 'Overview'},
      {id: 'details', label: 'Details'},
      {id: 'history', label: 'History'},
    ],
    activeTab: 'details',
  },
};

export const TwoTabs: Story = {
  args: {
    tabs: [
      {id: 'holdings', label: 'Allocation'},
      {id: 'positions', label: 'Positions'},
    ],
    activeTab: 'holdings',
  },
};

export const NoActiveTab: Story = {
  args: {
    tabs: [
      {id: 'a', label: 'Alpha'},
      {id: 'b', label: 'Beta'},
      {id: 'c', label: 'Gamma'},
    ],
    activeTab: '',
  },
};

export const WithPanelContent: Story = {
  render: () => ({
    template: `
      <cmn-tab-group [tabs]="tabs" [(activeTab)]="active">
        @if (active === 'overview') {
          <div class="p-cmn-4 text-cmn-sm text-text-secondary">Overview panel — summary information.</div>
        }
        @if (active === 'details') {
          <div class="p-cmn-4 text-cmn-sm text-text-secondary">Details panel — line-by-line breakdown.</div>
        }
        @if (active === 'history') {
          <div class="p-cmn-4 text-cmn-sm text-text-secondary">History panel — past events listed here.</div>
        }
      </cmn-tab-group>
    `,
    props: {
      tabs: [
        {id: 'overview', label: 'Overview'},
        {id: 'details', label: 'Details'},
        {id: 'history', label: 'History'},
      ],
      active: 'overview',
    },
  }),
};

export const HoldingsStyle: Story = {
  render: () => ({
    template: `
      <div class="rounded-cmn-md border border-border-default overflow-hidden">
        <cmn-tab-group [tabs]="tabs" [(activeTab)]="active">
          <div class="p-cmn-4">
            @if (active === 'holdings') {
              <p class="text-cmn-sm text-text-secondary">Allocation breakdown by institution.</p>
            }
            @if (active === 'positions') {
              <p class="text-cmn-sm text-text-secondary">Flat position-level view for brokerage + crypto.</p>
            }
          </div>
        </cmn-tab-group>
      </div>
    `,
    props: {
      tabs: [
        {id: 'holdings', label: 'Allocation'},
        {id: 'positions', label: 'Positions'},
      ],
      active: 'holdings',
    },
  }),
};

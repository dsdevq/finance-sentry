import type {Meta, StoryObj} from '@storybook/angular';

import {DisclosureRowComponent} from './disclosure-row.component';

const meta: Meta<DisclosureRowComponent> = {
  title: 'Components/DisclosureRow',
  component: DisclosureRowComponent,
  tags: ['autodocs'],
  argTypes: {
    label: {control: 'text'},
    sublabel: {control: 'text'},
    amount: {control: 'number'},
    currency: {control: 'text'},
    avatarName: {control: 'text'},
    open: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<DisclosureRowComponent>;

export const Default: Story = {
  args: {label: 'Chase Bank', open: true},
};

export const WithSublabel: Story = {
  args: {label: 'Chase Bank', sublabel: '3 accounts connected', open: true},
};

export const WithAvatar: Story = {
  args: {
    label: 'Chase Bank',
    sublabel: '3 accounts connected',
    avatarName: 'Chase Bank',
    open: true,
  },
};

export const WithAmount: Story = {
  args: {
    label: 'Wells Fargo',
    sublabel: '2 accounts connected',
    avatarName: 'Wells Fargo',
    amount: 12450.75,
    currency: 'USD',
    open: true,
  },
};

export const CollapsedInitially: Story = {
  args: {
    label: 'Vanguard',
    sublabel: '1 brokerage account',
    avatarName: 'Vanguard',
    amount: 98320.0,
    currency: 'USD',
    open: false,
  },
};

export const WithStatusIndicator: Story = {
  render: () => ({
    template: `
      <cmn-disclosure-row label="Monobank" sublabel="Reconnection required" avatarName="Monobank">
        <span status class="inline-flex items-center rounded-full bg-yellow-100 px-2 py-0.5 text-xs font-medium text-yellow-800">
          Needs attention
        </span>
        <p class="px-4 py-3 text-sm text-text-secondary">Account details appear here.</p>
      </cmn-disclosure-row>
    `,
  }),
};

export const WithActionButtons: Story = {
  render: () => ({
    template: `
      <cmn-disclosure-row label="Binance" sublabel="Crypto holdings" avatarName="Binance" [amount]="4210.50" currency="USD">
        <div actions class="flex gap-2">
          <button class="text-xs text-text-secondary hover:text-text-primary px-2 py-1 rounded border border-border-default">Sync</button>
          <button class="text-xs text-red-500 hover:text-red-600 px-2 py-1 rounded border border-border-default">Disconnect</button>
        </div>
        <p class="px-4 py-3 text-sm text-text-secondary">Crypto holdings appear here.</p>
      </cmn-disclosure-row>
    `,
  }),
};

export const FullFeatures: Story = {
  render: () => ({
    template: `
      <div class="border border-border-default rounded-cmn-md overflow-hidden">
        <cmn-disclosure-row label="Chase Bank" sublabel="3 accounts connected" avatarName="Chase Bank" [amount]="8540.22" currency="USD">
          <span status class="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
            Active
          </span>
          <div actions class="flex gap-2">
            <button class="text-xs text-text-secondary hover:text-text-primary px-2 py-1 rounded border border-border-default">Sync</button>
            <button class="text-xs text-red-500 hover:text-red-600 px-2 py-1 rounded border border-border-default">Disconnect</button>
          </div>
          <div class="px-4 py-3 space-y-1">
            <p class="text-sm text-text-primary">Checking ••••4242 — $3,540.22</p>
            <p class="text-sm text-text-primary">Savings ••••8877 — $5,000.00</p>
          </div>
        </cmn-disclosure-row>
        <cmn-disclosure-row label="Wells Fargo" sublabel="1 account connected" avatarName="Wells Fargo" [amount]="12450.00" currency="USD" [open]="false">
          <span status class="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
            Active
          </span>
          <div actions class="flex gap-2">
            <button class="text-xs text-red-500 hover:text-red-600 px-2 py-1 rounded border border-border-default">Disconnect</button>
          </div>
          <div class="px-4 py-3">
            <p class="text-sm text-text-primary">Checking ••••1234 — $12,450.00</p>
          </div>
        </cmn-disclosure-row>
      </div>
    `,
  }),
};

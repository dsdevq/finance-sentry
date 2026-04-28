import type {Meta, StoryObj} from '@storybook/angular';

import {SelectableCardComponent} from './selectable-card.component';

const meta: Meta<SelectableCardComponent> = {
  title: 'Components/SelectableCard',
  component: SelectableCardComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<SelectableCardComponent>;

export const HorizontalProviderRow: Story = {
  render: () => ({
    template: `
      <div class="w-[480px] p-cmn-4 bg-surface-bg">
        <cmn-selectable-card orientation="horizontal">
          <div leading
               class="w-10 h-10 rounded-cmn-sm bg-surface-raised flex items-center justify-center font-semibold">
            P
          </div>
          <span class="font-medium text-text-primary">Plaid</span>
          <span class="text-cmn-xs text-text-secondary">Connect US, Canadian, or European banks</span>
        </cmn-selectable-card>
      </div>
    `,
  }),
};

export const HorizontalConnected: Story = {
  render: () => ({
    template: `
      <div class="w-[480px] p-cmn-4 bg-surface-bg">
        <cmn-selectable-card orientation="horizontal">
          <div leading
               class="w-10 h-10 rounded-cmn-sm bg-surface-raised flex items-center justify-center font-semibold">
            M
          </div>
          <span class="font-medium text-text-primary">Monobank</span>
          <span class="text-cmn-xs text-text-secondary">Connect Monobank cards using a personal API token</span>
          <span trailing
                class="text-cmn-xs text-status-success font-semibold uppercase tracking-wide">
            Connected
          </span>
        </cmn-selectable-card>
      </div>
    `,
  }),
};

export const VerticalTypeTile: Story = {
  render: () => ({
    template: `
      <div class="w-[480px] grid grid-cols-3 gap-cmn-3 p-cmn-4 bg-surface-bg">
        <cmn-selectable-card orientation="vertical">
          <div leading
               class="w-12 h-12 rounded-full bg-surface-raised flex items-center justify-center text-cmn-lg font-semibold">
            B
          </div>
          <span class="font-medium text-text-primary">Bank</span>
          <span class="text-cmn-xs text-text-secondary">Plaid · Monobank</span>
        </cmn-selectable-card>
        <cmn-selectable-card orientation="vertical">
          <div leading
               class="w-12 h-12 rounded-full bg-surface-raised flex items-center justify-center text-cmn-lg font-semibold">
            C
          </div>
          <span class="font-medium text-text-primary">Crypto</span>
          <span class="text-cmn-xs text-text-secondary">Binance</span>
        </cmn-selectable-card>
        <cmn-selectable-card orientation="vertical">
          <div leading
               class="w-12 h-12 rounded-full bg-surface-raised flex items-center justify-center text-cmn-lg font-semibold">
            B
          </div>
          <span class="font-medium text-text-primary">Brokerage</span>
          <span class="text-cmn-xs text-text-secondary">Interactive Brokers</span>
        </cmn-selectable-card>
      </div>
    `,
  }),
};

export const Selected: Story = {
  args: {selected: true, orientation: 'horizontal'},
  render: args => ({
    props: args,
    template: `
      <div class="w-[480px] p-cmn-4 bg-surface-bg">
        <cmn-selectable-card [selected]="selected" [orientation]="orientation">
          <span class="font-medium text-text-primary">Selected card</span>
          <span class="text-cmn-xs text-text-secondary">Indigo accent border + subtle bg</span>
        </cmn-selectable-card>
      </div>
    `,
  }),
};

export const Disabled: Story = {
  args: {disabled: true},
  render: args => ({
    props: args,
    template: `
      <div class="w-[480px] p-cmn-4 bg-surface-bg">
        <cmn-selectable-card [disabled]="disabled">
          <span class="font-medium text-text-primary">Disabled card</span>
          <span class="text-cmn-xs text-text-secondary">No hover, no click</span>
        </cmn-selectable-card>
      </div>
    `,
  }),
};

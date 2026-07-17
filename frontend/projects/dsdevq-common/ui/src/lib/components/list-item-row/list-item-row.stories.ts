import type {Meta, StoryObj} from '@storybook/angular';

import {ListItemRowComponent} from './list-item-row.component';

const meta: Meta<ListItemRowComponent> = {
  title: 'Components/ListItemRow',
  component: ListItemRowComponent,
  tags: ['autodocs'],
  argTypes: {
    label: {control: 'text'},
    sublabel: {control: 'text'},
    dimmed: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<ListItemRowComponent>;

export const Default: Story = {
  args: {label: 'Netflix'},
};

export const WithSublabel: Story = {
  args: {label: 'Netflix', sublabel: 'Video streaming'},
};

export const Dimmed: Story = {
  args: {label: 'Spotify', sublabel: 'Music', dimmed: true},
};

export const WithAvatar: Story = {
  render: () => ({
    template: `
      <cmn-list-item-row label="Netflix" sublabel="Video streaming">
        <div avatar style="background: #e50914"
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-cmn-md font-bold text-white">
          N
        </div>
      </cmn-list-item-row>
    `,
  }),
};

export const WithAllSlots: Story = {
  render: () => ({
    template: `
      <cmn-list-item-row label="Netflix" sublabel="Video streaming">
        <div avatar style="background: #e50914"
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-cmn-md font-bold text-white">
          N
        </div>
        <div meta class="min-w-[100px] text-center">
          <div class="text-cmn-xs text-text-secondary">Feb 1, 2026</div>
          <div class="text-[11px] text-text-disabled">in 14d</div>
        </div>
        <div amount class="min-w-[72px] text-right">
          <div class="font-mono text-cmn-sm font-semibold text-text-primary">$15.99</div>
          <div class="text-[10px] text-text-disabled">/ mo</div>
        </div>
        <div actions class="flex shrink-0 gap-cmn-1">
          <button class="px-cmn-2 py-1 text-cmn-xs rounded border border-border-default text-text-secondary hover:text-text-primary">Dismiss</button>
        </div>
      </cmn-list-item-row>
    `,
  }),
};

export const DimmedWithActions: Story = {
  render: () => ({
    template: `
      <cmn-list-item-row label="Spotify" sublabel="Music" [dimmed]="true">
        <div avatar style="background: #1db954"
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-cmn-md font-bold text-white">
          S
        </div>
        <div amount class="min-w-[72px] text-right">
          <div class="font-mono text-cmn-sm font-semibold text-text-primary">$9.99</div>
          <div class="text-[10px] text-text-disabled">/ mo</div>
        </div>
        <div actions class="flex shrink-0 gap-cmn-1">
          <button class="px-cmn-2 py-1 text-cmn-xs rounded border border-border-default text-text-secondary hover:text-text-primary">Restore</button>
        </div>
      </cmn-list-item-row>
    `,
  }),
};

export const MultipleRowsInCard: Story = {
  render: () => ({
    template: `
      <div class="border border-border-default rounded-cmn-md overflow-hidden">
        <cmn-list-item-row label="Netflix" sublabel="Video streaming">
          <div avatar style="background: #e50914"
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-cmn-md font-bold text-white">
            N
          </div>
          <div meta class="min-w-[100px] text-center">
            <div class="text-cmn-xs text-status-warning font-semibold">Jan 25, 2026</div>
            <div class="text-[11px] text-status-warning">in 4d</div>
          </div>
          <div amount class="min-w-[72px] text-right">
            <div class="font-mono text-cmn-sm font-semibold text-text-primary">$15.99</div>
            <div class="text-[10px] text-text-disabled">/ mo</div>
          </div>
          <div actions class="flex shrink-0 gap-cmn-1">
            <button class="px-cmn-2 py-1 text-cmn-xs rounded border border-border-default text-text-secondary">Dismiss</button>
          </div>
        </cmn-list-item-row>
        <cmn-list-item-row label="Spotify" sublabel="Music">
          <div avatar style="background: #1db954"
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-cmn-md font-bold text-white">
            S
          </div>
          <div meta class="min-w-[100px] text-center">
            <div class="text-cmn-xs text-text-secondary">Feb 5, 2026</div>
            <div class="text-[11px] text-text-disabled">in 15d</div>
          </div>
          <div amount class="min-w-[72px] text-right">
            <div class="font-mono text-cmn-sm font-semibold text-text-primary">$9.99</div>
            <div class="text-[10px] text-text-disabled">/ mo</div>
          </div>
          <div actions class="flex shrink-0 gap-cmn-1">
            <button class="px-cmn-2 py-1 text-cmn-xs rounded border border-border-default text-text-secondary">Dismiss</button>
          </div>
        </cmn-list-item-row>
        <cmn-list-item-row label="Adobe CC" sublabel="Creative suite" [dimmed]="true">
          <div avatar style="background: #ff0000"
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-cmn-md font-bold text-white">
            A
          </div>
          <div amount class="min-w-[72px] text-right">
            <div class="font-mono text-cmn-sm font-semibold text-text-primary">$54.99</div>
            <div class="text-[10px] text-text-disabled">/ mo</div>
          </div>
          <div actions class="flex shrink-0 gap-cmn-1">
            <button class="px-cmn-2 py-1 text-cmn-xs rounded border border-border-default text-text-secondary">Restore</button>
          </div>
        </cmn-list-item-row>
      </div>
    `,
  }),
};

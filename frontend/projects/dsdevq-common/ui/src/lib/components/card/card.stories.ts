import type {Meta, StoryObj} from '@storybook/angular';

import type {CardPadding} from './card.component';
import {CardComponent} from './card.component';

interface CardStoryArgs {
  padding: CardPadding;
  elevated: boolean;
}

const meta: Meta<CardStoryArgs> = {
  title: 'Components/Card',
  component: CardComponent,
  tags: ['autodocs'],
  argTypes: {
    padding: {
      control: 'select',
      options: ['none', 'sm', 'md', 'lg'],
    },
    elevated: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<CardStoryArgs>;

export const Default: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-card [padding]="padding" [elevated]="elevated"><p class="text-text-primary">Card content goes here.</p></cmn-card>',
  }),
  args: {padding: 'md', elevated: false},
};

export const Elevated: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-card [padding]="padding" [elevated]="elevated"><p class="text-text-primary">Elevated card with shadow.</p></cmn-card>',
  }),
  args: {padding: 'md', elevated: true},
};

export const NoPadding: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-card padding="none" [elevated]="elevated"><div class="bg-accent-100 p-cmn-4 rounded-cmn-md"><p class="text-text-primary">No padding — content flush with card edge.</p></div></cmn-card>',
  }),
  args: {elevated: false},
};

export const AllPaddingSizes: Story = {
  render: () => ({
    template: `
      <div class="flex flex-col gap-cmn-4">
        <cmn-card padding="sm"><p class="text-text-secondary text-cmn-sm">padding=sm</p></cmn-card>
        <cmn-card padding="md"><p class="text-text-secondary text-cmn-sm">padding=md (default)</p></cmn-card>
        <cmn-card padding="lg"><p class="text-text-secondary text-cmn-sm">padding=lg</p></cmn-card>
      </div>
    `,
  }),
};

export const WithNestedContent: Story = {
  render: () => ({
    template: `
      <cmn-card padding="md" [elevated]="true">
        <h3 class="text-text-primary font-headline text-cmn-lg font-semibold mb-cmn-2">Account Balance</h3>
        <p class="text-text-secondary text-cmn-sm">Checking · ••••4242</p>
        <p class="text-accent-default font-headline text-cmn-2xl font-bold mt-cmn-2">$12,450.00</p>
      </cmn-card>
    `,
  }),
};

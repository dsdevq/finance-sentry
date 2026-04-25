import type {Meta, StoryObj} from '@storybook/angular';

import type {BadgeVariant} from './badge.component';
import {BadgeComponent} from './badge.component';

const meta: Meta<{variant: BadgeVariant}> = {
  title: 'Components/Badge',
  component: BadgeComponent,
  tags: ['autodocs'],
  argTypes: {
    variant: {control: 'select', options: ['success', 'error', 'warning', 'info', 'neutral']},
  },
};

export default meta;
type Story = StoryObj<{variant: BadgeVariant}>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: '<cmn-badge [variant]="variant">Neutral</cmn-badge>',
  }),
  args: {variant: 'neutral'},
};

export const AllVariants: Story = {
  render: () => ({
    template: `
      <div class="flex flex-wrap gap-2">
        <cmn-badge variant="success">Synced</cmn-badge>
        <cmn-badge variant="error">Error</cmn-badge>
        <cmn-badge variant="warning">Stale</cmn-badge>
        <cmn-badge variant="info">Info</cmn-badge>
        <cmn-badge variant="neutral">Pending</cmn-badge>
      </div>
    `,
  }),
};

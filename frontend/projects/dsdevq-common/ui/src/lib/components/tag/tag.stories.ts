import type {Meta, StoryObj} from '@storybook/angular';

import type {TagVariant} from './tag.component';
import {TagComponent} from './tag.component';

const meta: Meta<{variant: TagVariant}> = {
  title: 'Components/Tag',
  component: TagComponent,
  tags: ['autodocs'],
  argTypes: {
    variant: {control: 'select', options: ['success', 'error', 'warning', 'info', 'neutral']},
  },
};

export default meta;
type Story = StoryObj<{variant: TagVariant}>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: '<cmn-tag [variant]="variant">Neutral</cmn-tag>',
  }),
  args: {variant: 'neutral'},
};

export const AllVariants: Story = {
  render: () => ({
    template: `
      <div class="flex flex-wrap gap-2">
        <cmn-tag variant="success">Synced</cmn-tag>
        <cmn-tag variant="error">Error</cmn-tag>
        <cmn-tag variant="warning">Stale</cmn-tag>
        <cmn-tag variant="info">Info</cmn-tag>
        <cmn-tag variant="neutral">Pending</cmn-tag>
      </div>
    `,
  }),
};

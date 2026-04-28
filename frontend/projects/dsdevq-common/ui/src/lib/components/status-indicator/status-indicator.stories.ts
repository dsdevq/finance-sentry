import type {Meta, StoryObj} from '@storybook/angular';

import type {StatusIndicatorVariant} from './status-indicator.component';
import {StatusIndicatorComponent} from './status-indicator.component';

interface Args {
  variant: StatusIndicatorVariant;
  label: string;
  timestampLabel: string;
}

const meta: Meta<Args> = {
  title: 'Components/StatusIndicator',
  component: StatusIndicatorComponent,
  tags: ['autodocs'],
  argTypes: {
    variant: {control: 'select', options: ['success', 'warning', 'error', 'neutral']},
    label: {control: 'text'},
    timestampLabel: {control: 'text'},
  },
};

export default meta;
type Story = StoryObj<Args>;

export const Default: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-status-indicator [variant]="variant" [timestampLabel]="timestampLabel">{{ label }}</cmn-status-indicator>',
  }),
  args: {variant: 'success', label: 'Synced', timestampLabel: '3 mins ago'},
};

export const AllVariants: Story = {
  render: () => ({
    template: `
      <div class="flex flex-col gap-3">
        <cmn-status-indicator variant="success" timestampLabel="3 mins ago">Synced</cmn-status-indicator>
        <cmn-status-indicator variant="warning" timestampLabel="14h ago">Stale</cmn-status-indicator>
        <cmn-status-indicator variant="error" timestampLabel="2 days ago">Failed</cmn-status-indicator>
        <cmn-status-indicator variant="neutral">Pending</cmn-status-indicator>
      </div>
    `,
  }),
};

export const NoTimestamp: Story = {
  render: () => ({
    template:
      '<cmn-status-indicator variant="success">Synced</cmn-status-indicator>',
  }),
};

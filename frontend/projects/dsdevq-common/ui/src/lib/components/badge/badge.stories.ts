import type {Meta, StoryObj} from '@storybook/angular';
import {moduleMetadata} from '@storybook/angular';

import {IconComponent} from '../icon/icon.component';

import {BadgeComponent} from './badge.component';

const meta: Meta<BadgeComponent> = {
  title: 'Components/Badge',
  component: BadgeComponent,
  tags: ['autodocs'],
  decorators: [moduleMetadata({imports: [IconComponent]})],
};

export default meta;
type Story = StoryObj<BadgeComponent>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: `
      <cmn-badge [count]="count" [status]="status">
        <cmn-icon name="Bell" size="md" />
      </cmn-badge>
    `,
  }),
  args: {count: 5, status: 'error'},
};

export const Dot: Story = {
  render: () => ({
    template: `
      <cmn-badge [dot]="true" status="processing">
        <cmn-icon name="Bell" size="md" />
      </cmn-badge>
    `,
  }),
};

export const Standalone: Story = {
  render: () => ({
    template: `
      <div class="flex gap-2">
        <cmn-badge [count]="3" [standalone]="true" status="error" />
        <cmn-badge [count]="120" [standalone]="true" status="warning" />
        <cmn-badge [count]="0" [showZero]="true" [standalone]="true" status="success" />
      </div>
    `,
  }),
};

export const Statuses: Story = {
  render: () => ({
    template: `
      <div class="flex flex-wrap gap-6 items-center">
        <cmn-badge [count]="1" status="default"><cmn-icon name="Bell" size="md" /></cmn-badge>
        <cmn-badge [count]="1" status="success"><cmn-icon name="Bell" size="md" /></cmn-badge>
        <cmn-badge [count]="1" status="processing"><cmn-icon name="Bell" size="md" /></cmn-badge>
        <cmn-badge [count]="1" status="error"><cmn-icon name="Bell" size="md" /></cmn-badge>
        <cmn-badge [count]="1" status="warning"><cmn-icon name="Bell" size="md" /></cmn-badge>
      </div>
    `,
  }),
};

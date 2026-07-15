import type {Meta, StoryObj} from '@storybook/angular';

import {PageHeaderComponent} from './page-header.component';

const meta: Meta<PageHeaderComponent> = {
  title: 'Components/PageHeader',
  component: PageHeaderComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<PageHeaderComponent>;

export const TitleOnly: Story = {
  args: {title: 'Accounts'},
};

export const WithSubtitle: Story = {
  args: {title: 'Accounts', subtitle: 'Manage your connected bank and brokerage accounts'},
};

export const WithAction: Story = {
  args: {
    title: 'Accounts',
    subtitle: 'Manage your connected bank and brokerage accounts',
    actionLabel: 'Connect Account',
    actionIcon: 'Plus',
  },
};

export const ActionLoading: Story = {
  args: {
    title: 'Accounts',
    actionLabel: 'Connecting…',
    actionIcon: 'Plus',
    actionLoading: true,
  },
};

export const ActionDisabled: Story = {
  args: {
    title: 'Accounts',
    actionLabel: 'Connect Account',
    actionIcon: 'Plus',
    actionDisabled: true,
  },
};

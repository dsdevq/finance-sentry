import type {Meta, StoryObj} from '@storybook/angular';

import {AlertComponent} from './alert.component';

const meta: Meta<AlertComponent> = {
  title: 'Components/Alert',
  component: AlertComponent,
  tags: ['autodocs'],
  argTypes: {
    variant: {control: 'select', options: ['info', 'success', 'warning', 'error']},
    dismissible: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Info: Story = {
  render: () => ({
    template: '<cmn-alert variant="info">Your session will expire in 10 minutes.</cmn-alert>',
  }),
};

export const Success: Story = {
  render: () => ({
    template: '<cmn-alert variant="success">Account linked successfully.</cmn-alert>',
  }),
};

export const Warning: Story = {
  render: () => ({
    template: '<cmn-alert variant="warning">Your API key will expire in 3 days.</cmn-alert>',
  }),
};

export const Error: Story = {
  render: () => ({
    template:
      '<cmn-alert variant="error">Failed to sync transactions. Please try again.</cmn-alert>',
  }),
};

export const WithTitle: Story = {
  render: () => ({
    template:
      '<cmn-alert variant="error" title="Sync Failed">We couldn\'t connect to your bank. Check your credentials and try again.</cmn-alert>',
  }),
};

export const Dismissible: Story = {
  render: () => ({
    template:
      '<cmn-alert variant="info" [dismissible]="true">Click the × to dismiss this alert.</cmn-alert>',
  }),
};

export const WithLongContent: Story = {
  render: () => ({
    template: `
      <div class="max-w-md">
        <cmn-alert variant="warning" title="Security Notice" [dismissible]="true">
          We noticed a sign-in attempt from a new device in Chicago, IL. If this was you, no action is needed.
          If this wasn't you, please review your account security settings immediately and consider changing your password.
        </cmn-alert>
      </div>
    `,
  }),
};

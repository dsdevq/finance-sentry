import type {Meta, StoryObj} from '@storybook/angular';

import type {ToastVariant} from './toast.component';
import {ToastComponent} from './toast.component';

interface ToastStoryArgs {
  message: string;
  variant: ToastVariant;
}

const meta: Meta<ToastStoryArgs> = {
  title: 'Components/Toast',
  component: ToastComponent,
  tags: ['autodocs'],
  argTypes: {
    variant: {control: 'select', options: ['info', 'success', 'warning', 'error']},
    message: {control: 'text'},
  },
};

export default meta;
type Story = StoryObj<ToastStoryArgs>;

export const Info: Story = {
  render: args => ({
    props: args,
    template: '<cmn-toast [message]="message" [variant]="variant" />',
  }),
  args: {message: 'Your data has been saved.', variant: 'info'},
};

export const Success: Story = {
  render: args => ({
    props: args,
    template: '<cmn-toast [message]="message" [variant]="variant" />',
  }),
  args: {message: 'Account created successfully.', variant: 'success'},
};

export const Warning: Story = {
  render: args => ({
    props: args,
    template: '<cmn-toast [message]="message" [variant]="variant" />',
  }),
  args: {message: 'Session expires in 5 minutes.', variant: 'warning'},
};

export const Error: Story = {
  render: args => ({
    props: args,
    template: '<cmn-toast [message]="message" [variant]="variant" />',
  }),
  args: {message: 'Unable to connect to server. Please try again.', variant: 'error'},
};

export const AllVariants: Story = {
  render: () => ({
    template: `
      <div class="flex flex-col gap-cmn-3">
        <cmn-toast message="Info: Your profile has been updated." variant="info" />
        <cmn-toast message="Success: Payment processed successfully." variant="success" />
        <cmn-toast message="Warning: Unsaved changes will be lost." variant="warning" />
        <cmn-toast message="Error: Failed to load transactions." variant="error" />
      </div>
    `,
  }),
};

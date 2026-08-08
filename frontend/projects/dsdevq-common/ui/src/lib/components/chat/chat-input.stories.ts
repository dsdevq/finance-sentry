import type {Meta, StoryObj} from '@storybook/angular';

import {ChatInputComponent} from './chat-input.component';

const meta: Meta<ChatInputComponent> = {
  title: 'Components/Chat/ChatInput',
  component: ChatInputComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<ChatInputComponent>;

export const Default: Story = {
  args: {placeholder: 'Ask Ledger…'},
};

export const Loading: Story = {
  args: {loading: true, placeholder: 'Ledger is thinking…'},
};

export const Disabled: Story = {
  args: {disabled: true, placeholder: 'Agent not configured'},
};

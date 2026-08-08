import type {Meta, StoryObj} from '@storybook/angular';

import {ChatMessageComponent} from './chat-message.component';

const meta: Meta<ChatMessageComponent> = {
  title: 'Components/Chat/ChatMessage',
  component: ChatMessageComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<ChatMessageComponent>;

export const UserMessage: Story = {
  args: {role: 'user', text: "What's my allocation drift?"},
};

export const AssistantMessage: Story = {
  args: {
    role: 'assistant',
    text: 'You are 34% tech vs a 20% IPS target — 14pp over your rebalance band.',
  },
};

export const WithToolProgress: Story = {
  args: {
    role: 'assistant',
    text: 'Pulling your book…',
    streaming: true,
    tools: [
      {name: 'get_portfolio_snapshot', running: false},
      {name: 'get_allocation_vs_target', running: true},
    ],
  },
};

export const Thinking: Story = {
  args: {role: 'assistant', text: '', streaming: true},
};

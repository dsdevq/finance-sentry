import type {Meta, StoryObj} from '@storybook/angular';

import type {IconSize, LucideIconName} from './icon.component';
import {IconComponent} from './icon.component';

interface IconStoryArgs {
  name: LucideIconName;
  size: IconSize;
  color: string;
  ariaLabel: string;
}

const meta: Meta<IconStoryArgs> = {
  title: 'Components/Icon',
  component: IconComponent,
  tags: ['autodocs'],
  argTypes: {
    name: {control: 'text'},
    size: {control: 'select', options: ['sm', 'md', 'lg']},
    color: {control: 'color'},
    ariaLabel: {control: 'text'},
  },
};

export default meta;
type Story = StoryObj<IconStoryArgs>;

export const Default: Story = {
  args: {name: 'CircleCheck', size: 'md', color: 'currentColor', ariaLabel: ''},
};

export const Small: Story = {
  args: {name: 'CircleCheck', size: 'sm', color: 'currentColor', ariaLabel: ''},
};

export const Large: Story = {
  args: {name: 'CircleCheck', size: 'lg', color: 'currentColor', ariaLabel: ''},
};

export const WithColor: Story = {
  args: {name: 'Star', size: 'md', color: '#1e3a8a', ariaLabel: ''},
};

export const WithAriaLabel: Story = {
  args: {name: 'CircleAlert', size: 'md', color: 'currentColor', ariaLabel: 'Warning'},
};

export const AllSizes: Story = {
  render: () => ({
    template: `
      <div class="flex items-center gap-cmn-4">
        <cmn-icon name="TrendingUp" size="sm" />
        <cmn-icon name="TrendingUp" size="md" />
        <cmn-icon name="TrendingUp" size="lg" />
      </div>
    `,
  }),
};

export const CommonIcons: Story = {
  render: () => ({
    template: `
      <div class="flex items-center gap-cmn-4 flex-wrap">
        <cmn-icon name="Home" size="md" />
        <cmn-icon name="User" size="md" />
        <cmn-icon name="Settings" size="md" />
        <cmn-icon name="Bell" size="md" />
        <cmn-icon name="Search" size="md" />
        <cmn-icon name="TrendingUp" size="md" />
        <cmn-icon name="CreditCard" size="md" />
        <cmn-icon name="LogOut" size="md" />
      </div>
    `,
  }),
};

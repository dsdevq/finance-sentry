import type {Meta, StoryObj} from '@storybook/angular';

import type {IconSize} from './icon.component';
import {IconComponent} from './icon.component';

interface IconStoryArgs {
  name: string;
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
  args: {name: 'circle-check', size: 'md', color: 'currentColor', ariaLabel: ''},
};

export const Small: Story = {
  args: {name: 'circle-check', size: 'sm', color: 'currentColor', ariaLabel: ''},
};

export const Large: Story = {
  args: {name: 'circle-check', size: 'lg', color: 'currentColor', ariaLabel: ''},
};

export const WithColor: Story = {
  args: {name: 'star', size: 'md', color: '#1e3a8a', ariaLabel: ''},
};

export const WithAriaLabel: Story = {
  args: {name: 'alert-circle', size: 'md', color: 'currentColor', ariaLabel: 'Warning'},
};

export const UnknownIconRendersEmpty: Story = {
  args: {name: 'not-a-real-icon', size: 'md', color: 'currentColor', ariaLabel: ''},
};

export const AllSizes: Story = {
  render: () => ({
    template: `
      <div class="flex items-center gap-cmn-4">
        <cmn-icon name="trending-up" size="sm" />
        <cmn-icon name="trending-up" size="md" />
        <cmn-icon name="trending-up" size="lg" />
      </div>
    `,
  }),
};

export const CommonIcons: Story = {
  render: () => ({
    template: `
      <div class="flex items-center gap-cmn-4 flex-wrap">
        <cmn-icon name="home" size="md" />
        <cmn-icon name="user" size="md" />
        <cmn-icon name="settings" size="md" />
        <cmn-icon name="bell" size="md" />
        <cmn-icon name="search" size="md" />
        <cmn-icon name="trending-up" size="md" />
        <cmn-icon name="credit-card" size="md" />
        <cmn-icon name="log-out" size="md" />
      </div>
    `,
  }),
};

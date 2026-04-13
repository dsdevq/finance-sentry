import type {Meta, StoryObj} from '@storybook/angular';

import type {ButtonSize, ButtonType, ButtonVariant} from './button.component';
import {ButtonComponent} from './button.component';

interface ButtonStoryArgs {
  variant: ButtonVariant;
  size: ButtonSize;
  type: ButtonType;
  disabled: boolean;
  loading: boolean;
}

const meta: Meta<ButtonStoryArgs> = {
  title: 'Components/Button',
  component: ButtonComponent,
  tags: ['autodocs'],
  argTypes: {
    variant: {control: 'select', options: ['primary', 'secondary', 'destructive']},
    size: {control: 'select', options: ['sm', 'md', 'lg']},
    type: {control: 'select', options: ['button', 'submit', 'reset']},
    disabled: {control: 'boolean'},
    loading: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<ButtonStoryArgs>;

export const Primary: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-button [variant]="variant" [size]="size" [disabled]="disabled" [loading]="loading">Primary</cmn-button>',
  }),
  args: {variant: 'primary', size: 'md', disabled: false, loading: false},
};

export const Secondary: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-button variant="secondary" [size]="size" [disabled]="disabled">Secondary</cmn-button>',
  }),
  args: {size: 'md', disabled: false},
};

export const Destructive: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-button variant="destructive" [size]="size" [disabled]="disabled">Delete Account</cmn-button>',
  }),
  args: {size: 'md', disabled: false},
};

export const Loading: Story = {
  render: args => ({
    props: args,
    template: '<cmn-button variant="primary" [loading]="loading">Saving...</cmn-button>',
  }),
  args: {loading: true},
};

export const Disabled: Story = {
  render: args => ({
    props: args,
    template: '<cmn-button variant="primary" [disabled]="disabled">Disabled</cmn-button>',
  }),
  args: {disabled: true},
};

export const AllSizes: Story = {
  render: () => ({
    template: `
      <div class="flex items-center gap-cmn-4 flex-wrap">
        <cmn-button variant="primary" size="sm">Small</cmn-button>
        <cmn-button variant="primary" size="md">Medium</cmn-button>
        <cmn-button variant="primary" size="lg">Large</cmn-button>
      </div>
    `,
  }),
};

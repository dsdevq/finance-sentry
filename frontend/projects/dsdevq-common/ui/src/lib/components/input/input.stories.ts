import type {Meta, StoryObj} from '@storybook/angular';

import type {InputSize, InputType} from './input.component';
import {InputComponent} from './input.component';

interface InputStoryArgs {
  type: InputType;
  size: InputSize;
  hasError: boolean;
  readonly: boolean;
}

const meta: Meta<InputStoryArgs> = {
  title: 'Components/Input',
  component: InputComponent,
  tags: ['autodocs'],
  argTypes: {
    type: {control: 'select', options: ['text', 'email', 'password', 'number', 'tel', 'search']},
    size: {control: 'select', options: ['sm', 'md', 'lg']},
    hasError: {control: 'boolean'},
    readonly: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<InputStoryArgs>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: '<cmn-input [type]="type" [size]="size" placeholder="Enter text..." />',
  }),
  args: {type: 'text', size: 'md'},
};

export const WithValue: Story = {
  render: () => ({
    template: '<cmn-input type="text" size="md" [ngModel]="\'hello@example.com\'" />',
  }),
};

export const Disabled: Story = {
  render: () => ({
    template: '<cmn-input type="text" size="md" placeholder="Disabled input" [disabled]="true" />',
  }),
};

export const Readonly: Story = {
  render: () => ({
    template:
      '<cmn-input type="text" size="md" [readonly]="true" [ngModel]="\'Read-only value\'" />',
  }),
};

export const Error: Story = {
  render: () => ({
    template: '<cmn-input type="text" size="md" [hasError]="true" placeholder="Invalid input" />',
  }),
};

export const Password: Story = {
  render: () => ({
    template: '<cmn-input type="password" size="md" placeholder="Enter password" />',
  }),
};

export const AllSizes: Story = {
  render: () => ({
    template: `
      <div class="flex flex-col gap-cmn-3">
        <cmn-input size="sm" placeholder="Small input" />
        <cmn-input size="md" placeholder="Medium input (default)" />
        <cmn-input size="lg" placeholder="Large input" />
      </div>
    `,
  }),
};

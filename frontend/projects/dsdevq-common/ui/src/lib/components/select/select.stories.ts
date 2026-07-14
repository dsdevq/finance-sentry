import type {Meta, StoryObj} from '@storybook/angular';

import type {SelectMode, SelectOption, SelectSize} from './select.component';
import {SelectComponent} from './select.component';

interface SelectStoryArgs {
  options: SelectOption[];
  placeholder: string;
  size: SelectSize;
  mode: SelectMode;
  allowClear: boolean;
  showSearch: boolean;
  hasError: boolean;
}

const accountOptions: SelectOption[] = [
  {label: 'Main current account', value: 'main-current'},
  {label: 'Savings vault', value: 'savings-vault'},
  {label: 'Brokerage cash', value: 'brokerage-cash'},
];

const meta: Meta<SelectStoryArgs> = {
  title: 'Components/Select',
  component: SelectComponent,
  tags: ['autodocs'],
  argTypes: {
    size: {control: 'select', options: ['sm', 'md', 'lg']},
    mode: {control: 'select', options: ['default', 'multiple']},
    allowClear: {control: 'boolean'},
    showSearch: {control: 'boolean'},
    hasError: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<SelectStoryArgs>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: `
      <div class="w-80">
        <cmn-select
          [options]="options"
          [placeholder]="placeholder"
          [size]="size"
          [mode]="mode"
          [allowClear]="allowClear"
          [showSearch]="showSearch"
          [hasError]="hasError"
        />
      </div>
    `,
  }),
  args: {
    options: accountOptions,
    placeholder: 'Choose account',
    size: 'md',
    mode: 'default',
    allowClear: true,
    showSearch: false,
    hasError: false,
  },
};

export const Multiple: Story = {
  render: () => ({
    props: {options: accountOptions},
    template: `
      <div class="w-80">
        <cmn-select [options]="options" mode="multiple" placeholder="Choose accounts" />
      </div>
    `,
  }),
};

export const Error: Story = {
  render: () => ({
    props: {options: accountOptions},
    template: `
      <div class="w-80">
        <cmn-select [options]="options" [hasError]="true" placeholder="Required field" />
      </div>
    `,
  }),
};

import type {Meta, StoryObj} from '@storybook/angular';

import {EditableFieldComponent} from './editable-field.component';

const meta: Meta<EditableFieldComponent> = {
  title: 'Components/EditableField',
  component: EditableFieldComponent,
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: 'select',
      options: ['text', 'email', 'number', 'tel', 'url'],
    },
    value: {control: 'text'},
    placeholder: {control: 'text'},
    emptyLabel: {control: 'text'},
    ariaLabel: {control: 'text'},
    disabled: {control: 'boolean'},
  },
};

export default meta;
type Story = StoryObj<EditableFieldComponent>;

export const Default: Story = {
  args: {
    value: 'Emergency fund',
    ariaLabel: 'account label',
    placeholder: 'Enter label',
  },
};

export const Empty: Story = {
  args: {
    value: '',
    emptyLabel: 'No nickname',
    ariaLabel: 'account nickname',
    placeholder: 'Add nickname',
  },
};

export const Email: Story = {
  args: {
    value: 'denys@example.com',
    type: 'email',
    ariaLabel: 'notification email',
    placeholder: 'email@example.com',
  },
};

export const Disabled: Story = {
  args: {
    value: 'Locked value',
    ariaLabel: 'locked value',
    disabled: true,
  },
};

export const InCard: Story = {
  render: () => ({
    template: `
      <div class="max-w-md rounded-cmn-md border border-border-default bg-surface-card p-cmn-4">
        <p class="mb-cmn-2 text-cmn-xs font-medium uppercase text-text-secondary">Budget label</p>
        <cmn-editable-field
          [(value)]="value"
          ariaLabel="budget label"
          placeholder="Add budget label"
        />
      </div>
    `,
    props: {
      value: 'Monthly essentials',
    },
  }),
};

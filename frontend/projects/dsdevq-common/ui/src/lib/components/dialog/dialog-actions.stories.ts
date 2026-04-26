import type {Meta, StoryObj} from '@storybook/angular';

import {DialogActionsComponent} from './dialog-actions.component';

const meta: Meta<DialogActionsComponent> = {
  title: 'Components/DialogActions',
  component: DialogActionsComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<DialogActionsComponent>;

export const Between: Story = {
  args: {align: 'between'},
  render: args => ({
    props: args,
    template: `
      <div class="w-[480px] bg-surface-card border border-border-default rounded-cmn-lg p-cmn-5">
        <p class="text-cmn-sm text-text-secondary mb-cmn-4">Body content goes here.</p>
        <cmn-dialog-actions [align]="align">
          <cmn-button variant="secondary">Cancel</cmn-button>
          <cmn-button variant="primary">Confirm</cmn-button>
        </cmn-dialog-actions>
      </div>
    `,
  }),
};

export const End: Story = {
  args: {align: 'end'},
  render: args => ({
    props: args,
    template: `
      <div class="w-[480px] bg-surface-card border border-border-default rounded-cmn-lg p-cmn-5">
        <p class="text-cmn-sm text-text-secondary mb-cmn-4">Confirmation prompt.</p>
        <cmn-dialog-actions [align]="align">
          <cmn-button variant="secondary">Cancel</cmn-button>
          <cmn-button variant="destructive">Disconnect</cmn-button>
        </cmn-dialog-actions>
      </div>
    `,
  }),
};

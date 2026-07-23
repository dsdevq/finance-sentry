import type {Meta, StoryObj} from '@storybook/angular';
import {moduleMetadata} from '@storybook/angular';

import {ChipComponent} from '../chip/chip.component';
import {SectionHeaderComponent} from './section-header.component';

const meta: Meta<SectionHeaderComponent> = {
  title: 'Components/SectionHeader',
  component: SectionHeaderComponent,
  tags: ['autodocs'],
  decorators: [moduleMetadata({imports: [ChipComponent]})],
};

export default meta;
type Story = StoryObj<SectionHeaderComponent>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: `
      <cmn-section-header [label]="label">
        <span style="font-size:12px;color:#888">3 items</span>
      </cmn-section-header>
    `,
  }),
  args: {label: 'Banking Institutions'},
};

export const WithSortControls: Story = {
  render: args => ({
    props: args,
    template: `
      <cmn-section-header [label]="label">
        <div style="display:flex;gap:4px">
          <cmn-chip [selected]="true">Date</cmn-chip>
          <cmn-chip>Amount</cmn-chip>
        </div>
      </cmn-section-header>
    `,
  }),
  args: {label: 'Active · 5'},
};

export const LabelOnly: Story = {
  render: args => ({
    props: args,
    template: '<cmn-section-header [label]="label" />',
  }),
  args: {label: 'Detailed Holdings'},
};

export const TightGrouping: Story = {
  render: args => ({
    props: args,
    template: `
      <cmn-section-header [label]="label" align="start" labelAs="span">
        <button style="font-size:12px;padding:2px 8px;border:1px solid #ccc;border-radius:9999px">
          Category: Food &amp; Drink ✕
        </button>
      </cmn-section-header>
    `,
  }),
  args: {label: 'Filter'},
};

import type {Meta, StoryObj} from '@storybook/angular';

import type {InstitutionAvatarSize} from './institution-avatar.component';
import {InstitutionAvatarComponent} from './institution-avatar.component';

interface Args {
  name: string;
  size: InstitutionAvatarSize;
}

const meta: Meta<Args> = {
  title: 'Components/InstitutionAvatar',
  component: InstitutionAvatarComponent,
  tags: ['autodocs'],
  argTypes: {
    name: {control: 'text'},
    size: {control: 'select', options: ['sm', 'md', 'lg']},
  },
};

export default meta;
type Story = StoryObj<Args>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: '<cmn-institution-avatar [name]="name" [size]="size" />',
  }),
  args: {name: 'Wells Fargo', size: 'md'},
};

export const AllSizes: Story = {
  render: () => ({
    template: `
      <div class="flex items-center gap-3">
        <cmn-institution-avatar name="Chase Bank" size="sm" />
        <cmn-institution-avatar name="Wells Fargo" size="md" />
        <cmn-institution-avatar name="Vanguard" size="lg" />
      </div>
    `,
  }),
};

export const InstitutionGallery: Story = {
  render: () => ({
    template: `
      <div class="flex items-center gap-3">
        <cmn-institution-avatar name="Chase Bank" />
        <cmn-institution-avatar name="Wells Fargo" />
        <cmn-institution-avatar name="Vanguard" />
        <cmn-institution-avatar name="Fidelity" />
        <cmn-institution-avatar name="Coinbase" />
        <cmn-institution-avatar name="Kraken" />
      </div>
    `,
  }),
};

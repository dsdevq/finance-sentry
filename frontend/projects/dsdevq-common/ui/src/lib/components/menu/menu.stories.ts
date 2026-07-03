import type {Meta, StoryObj} from '@storybook/angular';

import {MenuComponent, type MenuItem} from './menu.component';

interface MenuStoryArgs {
  items: MenuItem[];
}

const meta: Meta<MenuStoryArgs> = {
  title: 'Components/Menu',
  component: MenuComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<MenuStoryArgs>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: `
      <div class="flex h-48 items-start justify-end p-cmn-8 bg-surface-bg">
        <cmn-menu
          [items]="items"
          ariaLabel="Account menu"
          triggerClass="h-8 w-8 rounded-cmn-full bg-accent-default text-cmn-xs font-semibold text-text-inverse hover:opacity-90"
        >
          D
        </cmn-menu>
      </div>
    `,
  }),
  args: {
    items: [
      {id: 'settings', label: 'Settings', icon: 'Settings2'},
      {id: 'logout', label: 'Log out', icon: 'LogOut', destructive: true},
    ],
  },
};

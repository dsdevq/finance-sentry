import type {Meta, StoryObj} from '@storybook/angular';

import {SidebarNavComponent} from './sidebar-nav.component';

const NAV_ITEMS = [
  {label: 'Dashboard', icon: 'LayoutDashboard', route: '/dashboard'},
  {label: 'Accounts', icon: 'Building2', route: '/accounts'},
  {label: 'Transactions', icon: 'ArrowLeftRight', route: '/transactions'},
  {label: 'Holdings', icon: 'BarChart2', route: '/holdings'},
  {label: 'Settings', icon: 'Settings', route: '/settings'},
];

const meta: Meta<SidebarNavComponent> = {
  title: 'Components/SidebarNav',
  component: SidebarNavComponent,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<SidebarNavComponent>;

export const Default: Story = {
  args: {items: NAV_ITEMS, activeRoute: '/dashboard'},
};

export const ActiveAccounts: Story = {
  args: {items: NAV_ITEMS, activeRoute: '/accounts'},
};

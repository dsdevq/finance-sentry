import type {Meta, StoryObj} from '@storybook/angular';

import type {PageContainerSpacing} from './page-container.component';
import {PageContainerComponent} from './page-container.component';

interface PageContainerStoryArgs {
  maxWidth: string;
  spacing: PageContainerSpacing;
}

const meta: Meta<PageContainerStoryArgs> = {
  title: 'Components/PageContainer',
  component: PageContainerComponent,
  tags: ['autodocs'],
  argTypes: {
    spacing: {
      control: 'select',
      options: ['md', 'lg'] satisfies PageContainerSpacing[],
    },
  },
};

export default meta;
type Story = StoryObj<PageContainerStoryArgs>;

export const Default: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-page-container [maxWidth]="maxWidth" [spacing]="spacing"><p class="text-text-primary">Page content goes here.</p></cmn-page-container>',
  }),
  args: {maxWidth: 'max-w-screen-lg', spacing: 'md'},
};

export const NarrowMaxWidth: Story = {
  render: args => ({
    props: args,
    template:
      '<cmn-page-container [maxWidth]="maxWidth" [spacing]="spacing"><p class="text-text-primary">Narrow page content (e.g. Settings at 900px).</p></cmn-page-container>',
  }),
  args: {maxWidth: 'max-w-[900px]', spacing: 'lg'},
};

export const WithRealisticContent: Story = {
  render: () => ({
    template: `
      <cmn-page-container>
        <div class="text-text-primary font-headline text-cmn-2xl font-semibold">Subscriptions</div>
        <p class="text-text-secondary text-cmn-sm">Detected recurring charges from your transactions.</p>
        <div class="bg-surface-card rounded-cmn-md border border-border-default p-cmn-4">
          <p class="text-text-secondary">Subscription list would appear here.</p>
        </div>
      </cmn-page-container>
    `,
  }),
};

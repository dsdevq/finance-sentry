import type { StorybookConfig } from '@storybook/angular';

const config: StorybookConfig = {
  framework: {
    name: '@storybook/angular',
    options: {},
  },
  stories: ['../src/**/*.stories.ts'],
  // No .mdx — Angular 21 + Storybook 10 MDX bug (storybookjs/storybook#34084)
  addons: [],
};

export default config;

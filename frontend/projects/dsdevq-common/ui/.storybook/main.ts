import type {StorybookConfig} from '@storybook/angular';

const config: StorybookConfig = {
  framework: {
    name: '@storybook/angular',
    options: {},
  },
  stories: ['../src/**/*.stories.ts'],
  // No .mdx — Angular 21 + Storybook 10 MDX bug (storybookjs/storybook#34084)
  // addons: toolbar is built into Storybook 10 core — no addon-essentials needed
  addons: [],
};

export default config;

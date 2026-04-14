import type {Preview} from '@storybook/angular';

// theme.css is injected via angular.json styles array — do not import here,
// as Webpack does not have a PostCSS/Tailwind loader for raw CSS imports.

const preview: Preview = {
  globalTypes: {
    theme: {
      description: 'Color theme',
      defaultValue: 'light',
      toolbar: {
        title: 'Theme',
        icon: 'circlehollow',
        items: [
          {value: 'light', title: 'Light'},
          {value: 'dark', title: 'Dark'},
        ],
        dynamicTitle: true,
      },
    },
  },
  decorators: [
    (storyFn, context) => {
      const theme = (context.globals['theme'] as string) ?? 'light';
      document.documentElement.setAttribute('data-theme', theme);
      return storyFn();
    },
  ],
};

export default preview;

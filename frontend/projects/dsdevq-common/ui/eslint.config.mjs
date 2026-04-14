import rootConfig from '../../../eslint.config.mjs';
import { defineConfig } from 'eslint/config';

export default defineConfig(
  ...rootConfig,
  {
    files: ['projects/dsdevq-common/ui/src/**/*.ts'],
    rules: {
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'cmn',
          style: 'kebab-case',
        },
      ],
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'cmn',
          style: 'camelCase',
        },
      ],
    },
  }
);

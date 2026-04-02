// ESLint configuration for strict code quality
import tseslint from 'typescript-eslint';
import {defineConfig} from 'eslint/config';

export default defineConfig(
  {
    ignores: ['projects/**/*', 'coverage/**', 'dist/**', '**/*.html'],
  },
  {
    files: ['**/*.ts'],
    extends: [...tseslint.configs.recommended],
    languageOptions: {
      parserOptions: {
        project: ['tsconfig.json', 'tsconfig.spec.json'],
      },
    },
    rules: {
      '@typescript-eslint/explicit-member-accessibility': [
        'error',
        {
          accessibility: 'explicit',
        },
      ],
      '@typescript-eslint/explicit-function-return-type': [
        'error',
        {
          allowExpressions: true,
        },
      ],
      '@typescript-eslint/member-ordering': [
        'error',
        {
          default: [
            'public-static-field',
            'protected-static-field',
            'private-static-field',
            'public-instance-field',
            'protected-instance-field',
            'private-instance-field',
            'constructor',
            'public-static-method',
            'protected-static-method',
            'private-static-method',
            'public-instance-method',
            'protected-instance-method',
            'private-instance-method',
          ],
        },
      ],
      'no-console': 'warn',
      'no-debugger': 'warn',
    },
  },
  {
    files: ['**/*.html'],
    rules: {},
  }
);

import {createRequire} from 'node:module';
import eslint from '@eslint/js';
import tsEslint from 'typescript-eslint';
import angular from 'angular-eslint';
import {defineConfig} from 'eslint/config';
import eslintConfigPrettier from 'eslint-config-prettier';
import eslintPluginPrettier from 'eslint-plugin-prettier/recommended';

import simpleImportSort from 'eslint-plugin-simple-import-sort';
import importPlugin from 'eslint-plugin-import';
import unusedImports from 'eslint-plugin-unused-imports';

const createdRequire = createRequire(import.meta.url);
const noUnsanitized = createdRequire('eslint-plugin-no-unsanitized');

import rxjs from '@smarttools/eslint-plugin-rxjs';

import prettier from 'eslint-plugin-prettier';

export default defineConfig(
  {
    ignores: ['.angular/**', 'coverage/**', 'dist/**', 'codegen.ts', 'projects/**'],
  },
  {
    files: ['**/*.ts'],
    languageOptions: {
      parserOptions: {
        projectService: true,
      },
    },
    extends: [
      eslint.configs.recommended,
      eslintConfigPrettier,
      ...tsEslint.configs.recommendedTypeChecked,
      ...tsEslint.configs.stylistic,
      ...angular.configs.tsRecommended,
      eslintPluginPrettier,
    ],
    processor: angular.processInlineTemplates,
    plugins: {
      import: importPlugin,
      rxjs,
      'unused-imports': unusedImports,
      'simple-import-sort': simpleImportSort,
      'no-unsanitized': noUnsanitized,
    },
    rules: {
      'import/no-cycle': [
        'error',
        {
          ignoreExternal: true,
          maxDepth: 20,
        },
      ],

      '@typescript-eslint/consistent-type-imports': [
        'error',
        {
          fixStyle: 'inline-type-imports',
        },
      ],
      '@typescript-eslint/consistent-type-exports': [
        'error',
        {
          fixMixedExportsWithInlineTypeSpecifier: true,
        },
      ],

      'simple-import-sort/imports': 'error',
      'simple-import-sort/exports': 'error',

      'comma-dangle': [
        'error',
        {
          arrays: 'always-multiline',
          objects: 'always-multiline',
          imports: 'always-multiline',
          exports: 'always-multiline',
          functions: 'never',
        },
      ],

      'max-lines': [
        'error',
        {
          max: 1000,
          skipBlankLines: true,
          skipComments: true,
        },
      ],

      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'fns',
          style: 'kebab-case',
        },
      ],

      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'fns',
          style: 'camelCase',
        },
      ],

      '@angular-eslint/component-class-suffix': [
        'error',
        {
          suffixes: ['Component', 'Container', 'Page', 'Modal'],
        },
      ],
      '@angular-eslint/use-lifecycle-interface': 'error',
      '@angular-eslint/no-host-metadata-property': 'off',

      '@angular-eslint/prefer-on-push-component-change-detection': 'error',

      '@angular-eslint/prefer-signals': 'error',

      '@typescript-eslint/explicit-module-boundary-types': 'error',

      '@angular-eslint/no-duplicates-in-metadata-arrays': ['error'],
      '@typescript-eslint/explicit-member-accessibility': [
        'error',
        {
          accessibility: 'explicit',

          overrides: {
            methods: 'explicit',
            constructors: 'no-public',
            properties: 'explicit',
          },
        },
      ],

      '@typescript-eslint/array-type': [
        'error',
        {
          default: 'array',
        },
      ],

      '@typescript-eslint/consistent-type-assertions': [
        'error',
        {
          assertionStyle: 'as',
          objectLiteralTypeAssertions: 'allow-as-parameter',
        },
      ],
      'dot-notation': 'off',
      '@typescript-eslint/dot-notation': [
        'error',
        {
          allowIndexSignaturePropertyAccess: true,
        },
      ],
      '@typescript-eslint/no-duplicate-enum-values': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/member-ordering': [
        'error',
        {
          default: [
            'signature',
            'private-decorated-field',
            'private-field',
            'public-decorated-field',
            'public-field',
            'protected-decorated-field',
            'protected-field',
            'constructor',
            'decorated-method',
            'static-method',
            'instance-method',
            'abstract-method',
            'method',
          ],
        },
      ],
      'no-magic-numbers': 'off',
      '@typescript-eslint/no-magic-numbers': [
        'error',
        {
          ignore: [0, 1, -1],
          ignoreArrayIndexes: true,
          enforceConst: true,
          detectObjects: false,
          ignoreEnums: true,
          ignoreNumericLiteralTypes: true,
          ignoreDefaultValues: true,
          ignoreReadonlyClassProperties: true,
        },
      ],
      '@typescript-eslint/naming-convention': [
        'error',
        {
          selector: 'objectLiteralProperty',
          format: ['UPPER_CASE', 'camelCase'],
        },
        {
          selector: 'classProperty',
          format: ['camelCase'],
        },
        {
          selector: 'classProperty',
          modifiers: ['static'],
          format: ['UPPER_CASE'],
        },
        {
          selector: 'classProperty',
          modifiers: ['readonly'],
          format: ['UPPER_CASE', 'PascalCase', 'camelCase'],
        },
        {
          selector: 'classProperty',
          modifiers: ['readonly'],
          format: ['UPPER_CASE', 'PascalCase'],
        },
        {
          selector: 'variable',
          format: ['UPPER_CASE', 'camelCase', 'PascalCase'],
        },
        {
          selector: ['enum', 'interface', 'class', 'typeAlias'],
          format: ['PascalCase'],
        },
        {
          selector: ['enumMember'],
          format: ['UPPER_CASE', 'camelCase', 'PascalCase'],
        },
      ],

      '@typescript-eslint/no-unused-vars': 'off',

      'unused-imports/no-unused-imports': 'error',
      'unused-imports/no-unused-vars': [
        'error',
        {
          argsIgnorePattern: '^_$',
          varsIgnorePattern: '^_$',
          caughtErrorsIgnorePattern: '^_$',
        },
      ],

      '@typescript-eslint/no-namespace': 'off',
      '@typescript-eslint/no-non-null-assertion': 'error',
      '@typescript-eslint/no-parameter-properties': 'off',
      '@typescript-eslint/no-redundant-type-constituents': 'off',
      '@typescript-eslint/no-shadow': 'error',
      '@typescript-eslint/no-use-before-define': 'error',
      '@typescript-eslint/no-floating-promises': 'warn',
      '@typescript-eslint/only-throw-error': 'warn',
      '@typescript-eslint/prefer-function-type': 'error',
      'no-case-declarations': 'off',

      '@typescript-eslint/unbound-method': [
        'error',
        {
          ignoreStatic: true,
        },
      ],

      quotes: [
        'error',
        'single',
        {
          avoidEscape: true,
        },
      ],

      semi: ['error', 'always'],

      curly: ['error', 'all'],
      '@typescript-eslint/triple-slash-reference': [
        'error',
        {
          path: 'always',
          types: 'prefer-import',
          lib: 'always',
        },
      ],

      'arrow-body-style': ['error', 'as-needed'],
      'no-console': [
        'error',
        {
          allow: ['warn', 'error'],
        },
      ],
      'no-param-reassign': [
        'error',
        {
          props: true,
          ignorePropertyModificationsFor: ['acc'],
        },
      ],

      'rxjs/no-unsafe-takeuntil': [
        'error',
        {
          alias: ['untilDestroyed'],
        },
      ],
      'rxjs/no-implicit-any-catch': 'off',
      'rxjs/no-nested-subscribe': 'error',
      'rxjs/finnish': [
        'error',
        {
          properties: true,
          variables: true,

          functions: false,
          methods: false,
          names: {
            '^(canActivate|canActivateChild|canDeactivate|canLoad|intercept|resolve|validate|store)$': false,
          },
          parameters: false,
          strict: false,
          types: {
            '^EventEmitter$': false,
          },
        },
      ],

      'no-restricted-syntax': [
        'error',
        {
          message: 'Usage of alert() is disalowed',
          selector: "CallExpression > Identifier[name='alert']",
        },
        {
          message: 'Methods decorated with @Output() should be readonly',
          selector:
            "PropertyDefinition[readonly!=true][decorators.0.expression.callee.name='Output']",
        },
        {
          message:
            "Properties 'currentValue | firstChange | previousValue | isFirstChange' should be optional",
          selector:
            "MethodDefinition[key.name='ngOnChanges'] MemberExpression[object.object.name='changes'][optional=false][property.name=/.*!/]",
        },
        {
          message: "ngOnChanges should have 'changes' as an argument",
          selector:
            "MethodDefinition[key.name='ngOnChanges'] > FunctionExpression > Identifier[name!='changes']",
        },
      ],

      'no-unsanitized/method': 'error',
      'no-unsanitized/property': 'error',
    },
  },
  {
    // Angular decorator metadata needs runtime class values for DI tokens.
    files: [
      '**/*.component.ts',
      '**/*.service.ts',
      '**/*.directive.ts',
      '**/*.pipe.ts',
      '**/*.guard.ts',
      '**/*.resolver.ts',
      '**/*.interceptor.ts',
      '**/*.effects.ts',
      '**/*.module.ts',
    ],
    rules: {
      '@typescript-eslint/consistent-type-imports': 'off',
    },
  },
  {
    // Signal store feature factories rely on inferred return types — annotating
    // them collapses method shapes to MethodsDictionary and breaks composition.
    files: ['**/store/*.ts'],
    rules: {
      '@typescript-eslint/explicit-module-boundary-types': 'off',
    },
  },
  {
    files: ['**/*.spec.ts'],
    rules: {
      'rxjs/finnish': 'off',
      'no-param-reassign': 'off',
      '@typescript-eslint/no-magic-numbers': 'off',
    },
  },
  {
    files: ['**/*.html'],
    plugins: {
      prettier,
    },
    extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility],
    rules: {
      'prettier/prettier': [
        'error',
        {
          parser: 'angular',
        },
      ],
      '@angular-eslint/template/prefer-control-flow': ['error'],
      '@angular-eslint/template/prefer-self-closing-tags': ['error'],
      '@angular-eslint/template/prefer-ngsrc': ['error'],
      '@angular-eslint/template/click-events-have-key-events': 'off',
      '@angular-eslint/template/interactive-supports-focus': 'off',
      '@angular-eslint/template/attributes-order': [
        'error',
        {
          order: [
            'STRUCTURAL_DIRECTIVE', // (*ngIf, *ngFor)
            'TEMPLATE_REFERENCE', // #myRef
            'INPUT_BINDING', // [value]="value"
            'TWO_WAY_BINDING', // [(ngModel)]="value"
            'OUTPUT_BINDING', // (click)
            'ATTRIBUTE_BINDING', // class, id, type
          ],
        },
      ],
    },
  }
);

import {createEslintConfig} from '@dsdevq-common/config/eslint';

export default createEslintConfig({
  selectorPrefix: 'fns',
  ignores: ['projects/**'],
});

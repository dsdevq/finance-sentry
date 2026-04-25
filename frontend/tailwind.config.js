/** @type {import('tailwindcss').Config} */
module.exports = {
  presets: [require('@dsdevq-common/config/tailwind')],
  content: ['./src/**/*.{ts,html}', './projects/**/*.{ts,html}'],
};

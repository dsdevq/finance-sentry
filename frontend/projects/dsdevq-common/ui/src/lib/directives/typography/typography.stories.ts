import {ChangeDetectionStrategy, Component} from '@angular/core';
import type {Meta, StoryObj} from '@storybook/angular';

import {TypographyDirective} from './typography.directive';

@Component({
  selector: 'cmn-typography-showcase',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TypographyDirective],
  template: `
    <div class="flex flex-col gap-cmn-4 p-cmn-6 bg-surface-bg">
      <p cmnTypography="display">Display — Finance Sentry</p>
      <p cmnTypography="h1">H1 — Account Overview</p>
      <p cmnTypography="h2">H2 — Recent Transactions</p>
      <p cmnTypography="h3">H3 — Portfolio Summary</p>
      <p cmnTypography="h4">H4 — Sub-section Title</p>
      <p cmnTypography="body">
        Body — The quick brown fox jumps over the lazy dog. This is regular body text used for
        descriptions and content.
      </p>
      <p cmnTypography="small">Small — Secondary information and supplemental text.</p>
      <p cmnTypography="caption">Caption — Last updated 3 minutes ago</p>
      <p cmnTypography="label">Label — Account Number</p>
      <p cmnTypography="code">Code — GET /api/v1/accounts</p>
    </div>
  `,
})
class TypographyShowcaseComponent {}

const meta: Meta = {
  title: 'Directives/Typography',
  component: TypographyShowcaseComponent,
};

export default meta;
type Story = StoryObj<typeof meta>;

export const AllLevels: Story = {};

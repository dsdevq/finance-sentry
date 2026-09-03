#!/usr/bin/env node
/**
 * Patches @lifekit-hq/ui@0.2.0 to add the `stacked` input to AreaChartComponent.
 *
 * The published 0.2.0 lacks this input; the library source is being updated in
 * lifekit-common. This script bridges the gap so the finance-sentry build passes
 * while we wait for the next published version. It is idempotent: re-running after
 * the library ships the input natively is a no-op.
 *
 * Applied by the `postinstall` hook so it runs after every `npm ci`.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const UI_DIR = path.join(__dirname, '../node_modules/@lifekit-hq/ui');
const FESM = path.join(UI_DIR, 'fesm2022/lifekit-hq-ui.mjs');
const TYPES = path.join(UI_DIR, 'types/lifekit-hq-ui.d.ts');

if (!fs.existsSync(FESM)) {
  // Package not installed (e.g. offline sandbox without NODE_AUTH_TOKEN) — skip silently.
  process.exit(0);
}

// ── Idempotency guard ────────────────────────────────────────────────────────
let fesm = fs.readFileSync(FESM, 'utf8');

// Check if the stacked input is already present in any form:
// - patch already applied to 0.2.0: both strings present
// - native 0.2.2+ build: has stacked in ɵcmp inputs declaration
const alreadyHasStacked =
  (fesm.includes('"stacked" }] : /* istanbul ignore next */ []));') &&
  fesm.includes('buildDatasets(series, stacked)')) ||
  // 0.2.2+ native: stacked appears in ɵcmp inputs before AreaChartComponent's ɵcmp
  fesm.includes('"stacked": { "alias": "stacked"; "required": false; "isSignal": true; }') ||
  fesm.includes('stacked: { classPropertyName: "stacked", publicName: "stacked", isSignal: true, isRequired: false, transformFunction: null } }, viewQueries: [{ propertyName: "canvasRef"');

if (alreadyHasStacked) {
  // Already patched (either by a prior postinstall run or because the package
  // already ships with the stacked input).
  console.log('✓ @lifekit-hq/ui: AreaChartComponent [stacked] input already present');
  process.exit(0);
}

// ── FESM bundle patches ──────────────────────────────────────────────────────

// 1. Add `stacked` input to the constructor and update the effect to track it.
fesm = fesm.replace(
  `        this.currency = input('USD', ...(ngDevMode ? [{ debugName: "currency" }] : /* istanbul ignore next */ []));
        effect(() => {
            const series = this.series();
            if (this.chart) {
                this.chart.data.labels = series[0]?.points.map(p => p.label) ?? [];
                this.chart.data.datasets = this.buildDatasets(series);
                this.chart.update('none');
            }
        });`,
  `        this.currency = input('USD', ...(ngDevMode ? [{ debugName: "currency" }] : /* istanbul ignore next */ []));
        this.stacked = input(true, ...(ngDevMode ? [{ debugName: "stacked" }] : /* istanbul ignore next */ []));
        effect(() => {
            const series = this.series();
            const stacked = this.stacked();
            if (this.chart) {
                this.chart.data.labels = series[0]?.points.map(p => p.label) ?? [];
                this.chart.data.datasets = this.buildDatasets(series, stacked);
                if (this.chart.options.scales) {
                    this.chart.options.scales['x'].stacked = stacked;
                    this.chart.options.scales['y'].stacked = stacked;
                }
                this.chart.update('none');
            }
        });`
);

// 2. Update buildDatasets to accept and apply the stacked flag.
fesm = fesm.replace(
  `    buildDatasets(series) {
        return series.map((s, i) => {
            const color = s.color ?? DEFAULT_SERIES_COLORS$1[i % DEFAULT_SERIES_COLORS$1.length];
            return {
                label: s.label,
                data: s.points.map(p => p.value),
                borderColor: color,
                backgroundColor: \`\${color}\${FILL_ALPHA}\`,
                borderWidth: 1.5,
                pointRadius: 0,
                pointHoverRadius: 4,
                fill: true,
                tension: 0.3,
            };
        });
    }`,
  `    buildDatasets(series, stacked) {
        return series.map((s, i) => {
            const color = s.color ?? DEFAULT_SERIES_COLORS$1[i % DEFAULT_SERIES_COLORS$1.length];
            return {
                label: s.label,
                data: s.points.map(p => p.value),
                borderColor: color,
                backgroundColor: \`\${color}\${FILL_ALPHA}\`,
                borderWidth: 1.5,
                pointRadius: 0,
                pointHoverRadius: 4,
                fill: stacked,
                tension: 0.3,
            };
        });
    }`
);

// 3. In buildChart, read the stacked flag and apply it to the initial Chart.js config.
fesm = fesm.replace(
  `        const currency = this.currency();
        const series = this.series();
        this.chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: series[0]?.points.map(p => p.label) ?? [],
                datasets: this.buildDatasets(series),
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                        align: 'end',
                        labels: {
                            color: textSecondary,
                            boxWidth: 10,
                            boxHeight: 10,
                            usePointStyle: true,
                            font: { family: 'Inter', size: 11 },
                        },
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        callbacks: {
                            label: tooltipCtx => \`\${tooltipCtx.dataset.label}: \${money(tooltipCtx.parsed.y, currency)}\`,
                            footer: items => {
                                const total = items.reduce((sum, item) => sum + item.parsed.y, 0);
                                return \`Total: \${money(total, currency)}\`;
                            },
                        },
                    },
                },
                scales: {
                    x: {
                        stacked: true,
                        grid: { display: false },
                        border: { display: false },
                        ticks: {
                            color: textSecondary,
                            font: { family: 'Inter', size: 11 },
                            maxRotation: 0,
                            autoSkip: true,
                            maxTicksLimit: 8,
                        },
                    },
                    y: {
                        stacked: true,
                        grid: { color: borderDefault },
                        border: { display: false },
                        ticks: {
                            color: textSecondary,
                            font: { family: 'Inter', size: 11 },
                            callback: val => money(val, currency, true),
                        },
                    },
                },
            },
        });`,
  `        const currency = this.currency();
        const series = this.series();
        const stacked = this.stacked();
        this.chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: series[0]?.points.map(p => p.label) ?? [],
                datasets: this.buildDatasets(series, stacked),
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                        align: 'end',
                        labels: {
                            color: textSecondary,
                            boxWidth: 10,
                            boxHeight: 10,
                            usePointStyle: true,
                            font: { family: 'Inter', size: 11 },
                        },
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        callbacks: {
                            label: tooltipCtx => \`\${tooltipCtx.dataset.label}: \${money(tooltipCtx.parsed.y, currency)}\`,
                            footer: items => {
                                const total = items.reduce((sum, item) => sum + item.parsed.y, 0);
                                return \`Total: \${money(total, currency)}\`;
                            },
                        },
                    },
                },
                scales: {
                    x: {
                        stacked: stacked,
                        grid: { display: false },
                        border: { display: false },
                        ticks: {
                            color: textSecondary,
                            font: { family: 'Inter', size: 11 },
                            maxRotation: 0,
                            autoSkip: true,
                            maxTicksLimit: 8,
                        },
                    },
                    y: {
                        stacked: stacked,
                        grid: { color: borderDefault },
                        border: { display: false },
                        ticks: {
                            color: textSecondary,
                            font: { family: 'Inter', size: 11 },
                            callback: val => money(val, currency, true),
                        },
                    },
                },
            },
        });`
);

// 4. Update the Angular ɵcmp inputs declaration.
fesm = fesm.replace(
  `inputs: { series: { classPropertyName: "series", publicName: "series", isSignal: true, isRequired: false, transformFunction: null }, label: { classPropertyName: "label", publicName: "label", isSignal: true, isRequired: false, transformFunction: null }, currency: { classPropertyName: "currency", publicName: "currency", isSignal: true, isRequired: false, transformFunction: null } }, viewQueries: [{ propertyName: "canvasRef", first: true, predicate: ["chartCanvas"], descendants: true, isSignal: true }], ngImport: i0, template: \`
    <div
      class="flex w-full flex-col gap-cmn-3 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4"
    >
      <span
        class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
      >
        {{ label() }}
      </span>
      <div class="relative h-64">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  \`, isInline: true, changeDetection: i0.ChangeDetectionStrategy.OnPush }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "21.2.21", ngImport: i0, type: AreaChartComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'cmn-area-chart',
                    changeDetection: ChangeDetectionStrategy.OnPush,
                    template: \`
    <div
      class="flex w-full flex-col gap-cmn-3 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4"
    >
      <span
        class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
      >
        {{ label() }}
      </span>
      <div class="relative h-64">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  \`,
                }]
        }], ctorParameters: () => [], propDecorators: { canvasRef: [{ type: i0.ViewChild, args: ['chartCanvas', { isSignal: true }] }], series: [{ type: i0.Input, args: [{ isSignal: true, alias: "series", required: false }] }], label: [{ type: i0.Input, args: [{ isSignal: true, alias: "label", required: false }] }], currency: [{ type: i0.Input, args: [{ isSignal: true, alias: "currency", required: false }] }] } });`,
  `inputs: { series: { classPropertyName: "series", publicName: "series", isSignal: true, isRequired: false, transformFunction: null }, label: { classPropertyName: "label", publicName: "label", isSignal: true, isRequired: false, transformFunction: null }, currency: { classPropertyName: "currency", publicName: "currency", isSignal: true, isRequired: false, transformFunction: null }, stacked: { classPropertyName: "stacked", publicName: "stacked", isSignal: true, isRequired: false, transformFunction: null } }, viewQueries: [{ propertyName: "canvasRef", first: true, predicate: ["chartCanvas"], descendants: true, isSignal: true }], ngImport: i0, template: \`
    <div
      class="flex w-full flex-col gap-cmn-3 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4"
    >
      <span
        class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
      >
        {{ label() }}
      </span>
      <div class="relative h-64">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  \`, isInline: true, changeDetection: i0.ChangeDetectionStrategy.OnPush }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "21.2.21", ngImport: i0, type: AreaChartComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'cmn-area-chart',
                    changeDetection: ChangeDetectionStrategy.OnPush,
                    template: \`
    <div
      class="flex w-full flex-col gap-cmn-3 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4"
    >
      <span
        class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
      >
        {{ label() }}
      </span>
      <div class="relative h-64">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  \`,
                }]
        }], ctorParameters: () => [], propDecorators: { canvasRef: [{ type: i0.ViewChild, args: ['chartCanvas', { isSignal: true }] }], series: [{ type: i0.Input, args: [{ isSignal: true, alias: "series", required: false }] }], label: [{ type: i0.Input, args: [{ isSignal: true, alias: "label", required: false }] }], currency: [{ type: i0.Input, args: [{ isSignal: true, alias: "currency", required: false }] }], stacked: [{ type: i0.Input, args: [{ isSignal: true, alias: "stacked", required: false }] }] } });`
);

fs.writeFileSync(FESM, fesm);

// ── TypeScript declaration patch ─────────────────────────────────────────────
if (!fs.existsSync(TYPES)) {
  process.exit(0);
}

let types = fs.readFileSync(TYPES, 'utf8');

// Add stacked field declaration.
types = types.replace(
  `    readonly currency: _angular_core.InputSignal<string>;
    constructor();
    ngAfterViewInit(): void;
    ngOnDestroy(): void;
    private buildDatasets;
    private buildChart;
    static ɵfac: _angular_core.ɵɵFactoryDeclaration<AreaChartComponent, never>;
    static ɵcmp: _angular_core.ɵɵComponentDeclaration<AreaChartComponent, "cmn-area-chart", never, { "series": { "alias": "series"; "required": false; "isSignal": true; }; "label": { "alias": "label"; "required": false; "isSignal": true; }; "currency": { "alias": "currency"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;`,
  `    readonly currency: _angular_core.InputSignal<string>;
    readonly stacked: _angular_core.InputSignal<boolean>;
    constructor();
    ngAfterViewInit(): void;
    ngOnDestroy(): void;
    private buildDatasets;
    private buildChart;
    static ɵfac: _angular_core.ɵɵFactoryDeclaration<AreaChartComponent, never>;
    static ɵcmp: _angular_core.ɵɵComponentDeclaration<AreaChartComponent, "cmn-area-chart", never, { "series": { "alias": "series"; "required": false; "isSignal": true; }; "label": { "alias": "label"; "required": false; "isSignal": true; }; "currency": { "alias": "currency"; "required": false; "isSignal": true; }; "stacked": { "alias": "stacked"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;`
);

fs.writeFileSync(TYPES, types);

console.log('✓ @lifekit-hq/ui patched: AreaChartComponent now accepts [stacked] input');

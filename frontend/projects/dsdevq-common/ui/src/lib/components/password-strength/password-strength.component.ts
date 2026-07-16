import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

const SEGMENT_COUNT = 4;

// Indexed by score (0–4); index 0 is the inactive fallback colour.
const SCORE_COLORS: readonly string[] = [
  'bg-border-default',
  'bg-red-500',
  'bg-amber-400',
  'bg-blue-500',
  'bg-green-500',
];

const SCORE_LABELS: readonly string[] = ['', 'Weak', 'Fair', 'Good', 'Strong'];

const SEGMENTS: readonly number[] = Array.from({length: SEGMENT_COUNT}, (_, i) => i + 1);

@Component({
  selector: 'cmn-password-strength',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (score() > 0) {
      <div>
        <div class="mb-cmn-1 flex gap-1">
          @for (seg of segments; track seg) {
            <div
              [class]="seg <= score() ? activeColor() : 'bg-border-default'"
              class="h-1 flex-1 rounded-full transition-colors"
            ></div>
          }
        </div>
        <span class="text-cmn-xs text-text-secondary">{{ label() }}</span>
      </div>
    }
  `,
})
export class CmnPasswordStrengthComponent {
  public readonly score = input.required<number>();

  protected readonly segments = SEGMENTS;
  protected readonly activeColor = computed(
    () => SCORE_COLORS[this.score()] ?? 'bg-border-default'
  );
  protected readonly label = computed(() => SCORE_LABELS[this.score()] ?? '');
}

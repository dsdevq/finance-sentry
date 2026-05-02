import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

const SCORE_COLORS: Record<number, string> = {
  1: 'bg-red-500',
  2: 'bg-amber-400',
  3: 'bg-blue-500',
  4: 'bg-green-500',
};

const SCORE_LABELS: Record<number, string> = {
  0: '',
  1: 'Weak',
  2: 'Fair',
  3: 'Good',
  4: 'Strong',
};

const SEGMENTS = [1, 2, 3, 4] as const;

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
  protected readonly activeColor = computed(() => SCORE_COLORS[this.score()] ?? 'bg-border-default');
  protected readonly label = computed(() => SCORE_LABELS[this.score()] ?? '');
}

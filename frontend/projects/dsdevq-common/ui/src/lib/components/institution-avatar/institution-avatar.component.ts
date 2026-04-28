import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

const MAX_INITIALS = 2;

const BASE_CLASSES =
  'inline-flex flex-shrink-0 items-center justify-center rounded-cmn-md border ' +
  'border-border-default bg-surface-card text-cmn-xs font-bold text-text-secondary ' +
  'shadow-sm';

const SIZE_CLASSES = {
  sm: 'h-6 w-6 text-[10px]',
  md: 'h-7 w-7 text-[10px]',
  lg: 'h-9 w-9 text-cmn-sm',
} as const;

export type InstitutionAvatarSize = keyof typeof SIZE_CLASSES;

@Component({
  selector: 'cmn-institution-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<span [class]="classes()" [attr.aria-label]="name()">{{ initials() }}</span>',
})
export class InstitutionAvatarComponent {
  public readonly name = input.required<string>();
  public readonly size = input<InstitutionAvatarSize>('md');

  public readonly initials = computed(() => InstitutionAvatarComponent.deriveInitials(this.name()));

  public readonly classes = computed(() => `${BASE_CLASSES} ${SIZE_CLASSES[this.size()]}`);

  private static deriveInitials(value: string): string {
    const trimmed = value?.trim() ?? '';
    if (trimmed.length === 0) {
      return '';
    }
    const words = trimmed.split(/\s+/).filter(Boolean);
    if (words.length === 1) {
      return words[0].slice(0, MAX_INITIALS).toUpperCase();
    }
    return words
      .slice(0, MAX_INITIALS)
      .map(word => word.charAt(0))
      .join('')
      .toUpperCase();
  }
}

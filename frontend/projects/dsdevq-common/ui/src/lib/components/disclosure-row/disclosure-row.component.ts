import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, input} from '@angular/core';

import {InstitutionAvatarComponent} from '../institution-avatar/institution-avatar.component';

@Component({
  selector: 'cmn-disclosure-row',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [InstitutionAvatarComponent, DecimalPipe],
  host: {style: 'display: block'},
  template: `
    <details [open]="open()" class="group border-b border-border-default last:border-0">
      <summary
        class="flex items-center justify-between gap-cmn-4 px-cmn-4 py-cmn-4 cursor-pointer hover:bg-surface-bg transition-colors"
      >
        <div class="flex items-center gap-cmn-3 flex-1 min-w-0">
          @if (avatarName()) {
            <cmn-institution-avatar [name]="avatarName()!" [logoUrl]="avatarLogoUrl()" />
          }
          <div class="min-w-0">
            <p class="font-medium text-text-primary">{{ label() }}</p>
            @if (sublabel()) {
              <p class="text-cmn-xs text-text-secondary">{{ sublabel() }}</p>
            }
          </div>
        </div>
        <ng-content select="[status]" />
        @if (amount() !== null) {
          <div class="font-mono tabular-nums font-medium text-text-primary text-right min-w-[7rem]">
            @if (currency()) {
              {{ currency() }}
            }
            {{ amount() | number: '1.2-2' }}
          </div>
        }
        <ng-content select="[actions]" />
        <svg
          class="w-4 h-4 text-text-secondary transition-transform group-open:rotate-180 shrink-0"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M19 9l-7 7-7-7"
          />
        </svg>
      </summary>
      <ng-content />
    </details>
  `,
})
export class DisclosureRowComponent {
  public readonly label = input.required<string>();
  public readonly sublabel = input<string | null>(null);
  public readonly amount = input<number | null>(null);
  public readonly currency = input<string | null>(null);
  public readonly avatarName = input<string | null>(null);
  /** Optional remote logo for the avatar; falls back to initials if absent/broken. */
  public readonly avatarLogoUrl = input<string | null>(null);
  public readonly open = input<boolean>(true);
}

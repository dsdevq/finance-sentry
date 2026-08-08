import {ChangeDetectionStrategy, Component, computed, input, output, signal} from '@angular/core';

import {ButtonComponent} from '../button/button.component';
import {type LucideIconName} from '../icon/icon.component';

const TEXTAREA_CLASSES =
  'flex-1 resize-none rounded-cmn-md border border-border-default bg-surface-card ' +
  'px-cmn-3 py-cmn-2 text-cmn-md text-text-primary placeholder:text-text-disabled ' +
  'focus:outline-none focus:ring-2 focus:ring-border-focus focus:border-border-focus ' +
  'disabled:opacity-50 disabled:cursor-not-allowed transition-colors max-h-40';

@Component({
  selector: 'cmn-chat-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonComponent],
  template: `
    <form (submit)="onSubmit($event)" class="flex items-end gap-cmn-2">
      <textarea
        [class]="TEXTAREA_CLASSES"
        [value]="value()"
        [disabled]="disabled()"
        [placeholder]="placeholder()"
        [attr.aria-label]="placeholder()"
        (input)="onInput($event)"
        (keydown)="onKeydown($event)"
        rows="1"
      ></textarea>

      <cmn-button
        [icon]="sendIcon"
        [disabled]="!canSend()"
        [loading]="loading()"
        type="submit"
        variant="primary"
        aria-label="Send message"
      />
    </form>
  `,
})
export class ChatInputComponent {
  public readonly disabled = input<boolean>(false);
  public readonly loading = input<boolean>(false);
  public readonly placeholder = input<string>('Ask Ledger…');

  public readonly send = output<string>();

  public readonly canSend = computed(
    () => !this.disabled() && !this.loading() && this.value().trim().length > 0
  );

  protected readonly TEXTAREA_CLASSES = TEXTAREA_CLASSES;
  protected readonly sendIcon: LucideIconName = 'Send';
  protected readonly value = signal<string>('');

  public onInput(event: Event): void {
    this.value.set((event.target as HTMLTextAreaElement).value);
  }

  public onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.submit();
    }
  }

  public onSubmit(event: Event): void {
    event.preventDefault();
    this.submit();
  }

  private submit(): void {
    if (!this.canSend()) {
      return;
    }

    this.send.emit(this.value().trim());
    this.value.set('');
  }
}

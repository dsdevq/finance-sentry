import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

export type ChatMessageRole = 'user' | 'assistant';

/** A tool the assistant is calling / has called — drives progressive-feedback chips. */
export interface ChatToolActivity {
  name: string;
  running: boolean;
}

// The width cap lives on the column wrapper (a flex item in the w-full row → a *definite*
// percentage basis), not on the bubble. Capping an inline-block against a shrink-wrapped parent
// collapses `max-w-[85%]` toward zero and makes `break-words` wrap every character.
const BUBBLE_BASE =
  'w-fit rounded-cmn-lg px-cmn-4 py-cmn-3 text-cmn-md font-base whitespace-pre-wrap break-words';

const USER_BUBBLE = 'bg-accent-default text-text-inverse';
const ASSISTANT_BUBBLE = 'bg-surface-raised text-text-primary border border-border-default';

@Component({
  selector: 'cmn-chat-message',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="rowClasses()">
      <div [class.items-end]="isUser()" class="flex flex-col gap-cmn-2 max-w-[85%]">
        <div [class]="bubbleClasses()">
          @if (text()) {
            <span>{{ text() }}</span>
          }
          @if (streaming() && !text()) {
            <span class="inline-flex gap-1" aria-label="Thinking">
              <span class="h-1.5 w-1.5 rounded-full bg-text-secondary animate-pulse"></span>
              <span
                class="h-1.5 w-1.5 rounded-full bg-text-secondary animate-pulse [animation-delay:150ms]"
              ></span>
              <span
                class="h-1.5 w-1.5 rounded-full bg-text-secondary animate-pulse [animation-delay:300ms]"
              ></span>
            </span>
          }
          @if (streaming() && text()) {
            <span
              class="ml-0.5 inline-block h-4 w-1 align-middle bg-text-secondary animate-pulse"
            ></span>
          }
        </div>

        @if (tools().length > 0) {
          <div class="flex flex-wrap gap-cmn-1" role="status" aria-label="Tool activity">
            @for (tool of tools(); track tool.name) {
              <span
                [class.animate-pulse]="tool.running"
                class="inline-flex items-center gap-1 rounded-cmn-full bg-surface-hover px-cmn-2 py-0.5 text-cmn-xs font-medium text-text-secondary"
              >
                <span [class]="dotClass(tool.running)" class="h-1.5 w-1.5 rounded-full"></span>
                {{ tool.name }}
              </span>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class ChatMessageComponent {
  public readonly role = input<ChatMessageRole>('assistant');
  public readonly text = input<string>('');
  public readonly streaming = input<boolean>(false);
  public readonly tools = input<ChatToolActivity[]>([]);

  public readonly isUser = computed(() => this.role() === 'user');

  public readonly rowClasses = computed(() =>
    this.isUser() ? 'flex w-full justify-end' : 'flex w-full justify-start'
  );

  public readonly bubbleClasses = computed(
    () => `${BUBBLE_BASE} ${this.isUser() ? USER_BUBBLE : ASSISTANT_BUBBLE}`
  );

  public dotClass(running: boolean): string {
    return running ? 'bg-status-info' : 'bg-status-success';
  }
}

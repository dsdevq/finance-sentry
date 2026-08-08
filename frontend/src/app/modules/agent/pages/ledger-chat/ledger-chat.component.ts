import {
  afterRenderEffect,
  ChangeDetectionStrategy,
  Component,
  type ElementRef,
  inject,
  viewChild,
} from '@angular/core';
import {
  AlertComponent,
  ButtonComponent,
  ChatInputComponent,
  ChatMessageComponent,
  EmptyStateComponent,
  type LucideIconName,
} from '@dsdevq-common/ui';

import {type ConversationSummary} from '../../models/conversation/conversation.model';
import {AgentChatStore} from '../../store/agent-chat.store';

@Component({
  selector: 'fns-ledger-chat',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {class: 'block h-full'},
  providers: [AgentChatStore],
  imports: [
    AlertComponent,
    ButtonComponent,
    ChatInputComponent,
    ChatMessageComponent,
    EmptyStateComponent,
  ],
  templateUrl: './ledger-chat.component.html',
})
export class LedgerChatComponent {
  private readonly scrollContainer = viewChild<ElementRef<HTMLElement>>('scrollContainer');

  public readonly store = inject(AgentChatStore);

  protected readonly newChatIcon: LucideIconName = 'Plus';
  protected readonly deleteIcon: LucideIconName = 'Trash2';
  protected readonly emptyIcon: LucideIconName = 'Sparkles';

  constructor() {
    // Keep the newest message in view as text streams in (a render concern, not state).
    afterRenderEffect(() => {
      this.store.messages();
      const element = this.scrollContainer()?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }

  public onSend(text: string): void {
    this.store.send(text);
  }

  public onNewChat(): void {
    this.store.resetThread();
  }

  public onSelect(conversation: ConversationSummary): void {
    this.store.selectConversation(conversation.id);
  }

  public onDelete(conversation: ConversationSummary, event: Event): void {
    event.stopPropagation();
    this.store.deleteConversation(conversation.id);
  }
}

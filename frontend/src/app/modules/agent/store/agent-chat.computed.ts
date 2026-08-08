import {computed, type Signal} from '@angular/core';

import {type ChatMessageView} from '../models/chat/chat.model';
import {type ConversationSummary} from '../models/conversation/conversation.model';

interface StateSignals {
  conversations: Signal<ConversationSummary[]>;
  messages: Signal<ChatMessageView[]>;
  isStreaming: Signal<boolean>;
  activeConversationId: Signal<string | null>;
}

export function agentChatComputed(store: StateSignals) {
  return {
    hasConversations: computed(() => store.conversations().length > 0),
    hasMessages: computed(() => store.messages().length > 0),
    isEmpty: computed(() => store.messages().length === 0 && !store.isStreaming()),
    canSend: computed(() => !store.isStreaming()),
  };
}

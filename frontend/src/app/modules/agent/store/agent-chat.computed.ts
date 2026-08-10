import {computed, type Signal} from '@angular/core';

import {type ConversationSummary} from '../models/conversation/conversation.model';

interface StateSignals {
  conversations: Signal<ConversationSummary[]>;
  threadNonce: Signal<number>;
}

export function agentChatComputed(store: StateSignals) {
  return {
    hasConversations: computed(() => store.conversations().length > 0),
    // Structural key: remounts <cmn-chat> so it re-inits with fresh history on new-chat / switch.
    historyKey: computed(() => String(store.threadNonce())),
  };
}

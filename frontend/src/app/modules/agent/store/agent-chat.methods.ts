import {type ChatToolActivity} from '@dsdevq-common/ui';
import {getState, patchState, type WritableStateSource} from '@ngrx/signals';

import {type ChatMessageView} from '../models/chat/chat.model';
import {
  type AgentMessage,
  type ConversationSummary,
} from '../models/conversation/conversation.model';
import {type AgentChatState} from './agent-chat.state';

function updateLastAssistant(
  messages: ChatMessageView[],
  update: (message: ChatMessageView) => ChatMessageView
): ChatMessageView[] {
  const lastIndex = messages.map(m => m.role).lastIndexOf('assistant');
  if (lastIndex < 0) {
    return messages;
  }

  const next = [...messages];
  next[lastIndex] = update(next[lastIndex]);
  return next;
}

export function agentChatMethods(store: WritableStateSource<AgentChatState>) {
  return {
    setConversations(conversations: ConversationSummary[]): void {
      patchState(store, {conversations});
    },

    setActiveConversationId(activeConversationId: string | null): void {
      patchState(store, {activeConversationId});
    },

    setStreaming(isStreaming: boolean): void {
      patchState(store, {isStreaming});
    },

    resetThread(): void {
      patchState(store, {messages: [], activeConversationId: null});
    },

    loadHistory(messages: AgentMessage[]): void {
      const view: ChatMessageView[] = messages
        .filter(m => m.role === 'user' || m.role === 'assistant')
        .map(m => ({
          id: m.id,
          role: m.role as 'user' | 'assistant',
          text: m.content,
          streaming: false,
          tools: [],
        }));
      patchState(store, {messages: view});
    },

    appendUserMessage(text: string): void {
      const message: ChatMessageView = {
        id: crypto.randomUUID(),
        role: 'user',
        text,
        streaming: false,
        tools: [],
      };
      patchState(store, {messages: [...getState(store).messages, message]});
    },

    beginAssistantMessage(): void {
      const placeholder: ChatMessageView = {
        id: crypto.randomUUID(),
        role: 'assistant',
        text: '',
        streaming: true,
        tools: [],
      };
      patchState(store, {messages: [...getState(store).messages, placeholder]});
    },

    appendAssistantDelta(delta: string): void {
      const messages = updateLastAssistant(getState(store).messages, m => ({
        ...m,
        text: m.text + delta,
      }));
      patchState(store, {messages});
    },

    setToolActivity(name: string, phase: 'start' | 'end'): void {
      const messages = updateLastAssistant(getState(store).messages, m => {
        const tools = [...m.tools];
        const index = tools.findIndex(t => t.name === name);
        const running = phase === 'start';
        const activity: ChatToolActivity = {name, running};
        if (index >= 0) {
          tools[index] = activity;
        } else {
          tools.push(activity);
        }
        return {...m, tools};
      });
      patchState(store, {messages});
    },

    finishAssistantMessage(messageId: string): void {
      const messages = updateLastAssistant(getState(store).messages, m => ({
        ...m,
        id: messageId,
        streaming: false,
        tools: m.tools.map(t => ({...t, running: false})),
      }));
      patchState(store, {messages});
    },

    endStreamingOnError(): void {
      const state = getState(store);
      const lastIndex = state.messages.map(m => m.role).lastIndexOf('assistant');
      if (lastIndex < 0) {
        return;
      }

      const last = state.messages[lastIndex];
      // Drop an empty placeholder; otherwise just stop the streaming indicator.
      const messages =
        last.streaming && last.text.trim() === ''
          ? state.messages.filter((_, index) => index !== lastIndex)
          : updateLastAssistant(state.messages, m => ({
              ...m,
              streaming: false,
              tools: m.tools.map(t => ({...t, running: false})),
            }));
      patchState(store, {messages});
    },

    removeConversationLocally(id: string): void {
      const state = getState(store);
      patchState(store, {
        conversations: state.conversations.filter(c => c.id !== id),
        ...(state.activeConversationId === id ? {messages: [], activeConversationId: null} : {}),
      });
    },
  };
}

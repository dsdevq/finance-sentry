import {inject} from '@angular/core';
import {type CmnChatMessage, type CmnChatStreamEvent} from '@lifekit-hq/ui';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {
  catchError,
  EMPTY,
  filter,
  finalize,
  map,
  type Observable,
  pipe,
  switchMap,
  tap,
} from 'rxjs';

import {type AgentSseEvent} from '../models/chat/chat.model';
import {
  type AgentMessage,
  type ConversationSummary,
} from '../models/conversation/conversation.model';
import {AgentService} from '../services/agent.service';

interface EffectsStore {
  activeConversationId: () => string | null;
  setConversations: (conversations: ConversationSummary[]) => void;
  setActiveConversationId: (id: string | null) => void;
  openConversation: (id: string, history: CmnChatMessage[]) => void;
  removeConversationLocally: (id: string) => void;
}

/** Maps persisted history to Deep Chat's roles; tool rows aren't shown as chat bubbles. */
function toHistory(messages: AgentMessage[]): CmnChatMessage[] {
  return messages
    .filter(m => m.role === 'user' || m.role === 'assistant')
    .map(m => ({role: m.role === 'assistant' ? 'ai' : 'user', text: m.content}));
}

export function agentChatEffects(store: EffectsStore) {
  const agentService = inject(AgentService);

  const loadConversations = rxMethod<void>(
    pipe(
      switchMap(() =>
        agentService.listConversations().pipe(
          tap(conversations => store.setConversations(conversations)),
          catchError(() => EMPTY)
        )
      )
    )
  );

  const selectConversation = rxMethod<string>(
    pipe(
      switchMap(id =>
        agentService.getConversation(id).pipe(
          tap(detail => store.openConversation(detail.id, toHistory(detail.messages))),
          catchError(() => EMPTY)
        )
      )
    )
  );

  const deleteConversation = rxMethod<string>(
    pipe(
      switchMap(id =>
        agentService.deleteConversation(id).pipe(
          tap(() => {
            store.removeConversationLocally(id);
            loadConversations();
          }),
          catchError(() => EMPTY)
        )
      )
    )
  );

  // One assistant turn for <cmn-chat>. Threads the conversation id (captured mid-stream when the
  // server assigns a new thread) and refreshes the sidebar list once the turn settles.
  const stream = (text: string): Observable<CmnChatStreamEvent> =>
    agentService.streamChat({conversationId: store.activeConversationId(), message: text}).pipe(
      tap(event => {
        if (event.type === 'conversation') {
          store.setActiveConversationId(event.conversationId);
        }
      }),
      filter(
        (event): event is Extract<AgentSseEvent, {type: 'text'} | {type: 'error'}> =>
          event.type === 'text' || event.type === 'error'
      ),
      map(event =>
        event.type === 'text'
          ? ({type: 'text', delta: event.delta} as const)
          : ({type: 'error', message: event.message} as const)
      ),
      finalize(() => loadConversations())
    );

  return {loadConversations, selectConversation, deleteConversation, stream};
}

export function agentChatHooks(store: {loadConversations: () => void}) {
  store.loadConversations();
}

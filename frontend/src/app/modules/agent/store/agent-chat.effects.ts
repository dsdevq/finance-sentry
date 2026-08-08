import {inject} from '@angular/core';
import {extractErrorCode} from '@dsdevq-common/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, filter, finalize, pipe, switchMap, tap} from 'rxjs';

import {type AgentSseEvent} from '../models/chat/chat.model';
import {
  type AgentMessage,
  type ConversationSummary,
} from '../models/conversation/conversation.model';
import {AgentService} from '../services/agent.service';

interface EffectsStore {
  activeConversationId: () => string | null;
  canSend: () => boolean;
  setConversations: (conversations: ConversationSummary[]) => void;
  setActiveConversationId: (id: string | null) => void;
  setStreaming: (streaming: boolean) => void;
  resetThread: () => void;
  loadHistory: (messages: AgentMessage[]) => void;
  appendUserMessage: (text: string) => void;
  beginAssistantMessage: () => void;
  appendAssistantDelta: (delta: string) => void;
  setToolActivity: (name: string, phase: 'start' | 'end') => void;
  finishAssistantMessage: (messageId: string) => void;
  endStreamingOnError: () => void;
  removeConversationLocally: (id: string) => void;
  setError: (code: string | null) => void;
  setLoading: () => void;
  setSuccess: () => void;
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

  const handleEvent = (event: AgentSseEvent): void => {
    switch (event.type) {
      case 'conversation':
        store.setActiveConversationId(event.conversationId);
        break;
      case 'text':
        store.appendAssistantDelta(event.delta);
        break;
      case 'tool':
        store.setToolActivity(event.name, event.phase);
        break;
      case 'error':
        store.setError(event.code);
        store.endStreamingOnError();
        break;
      case 'done':
        store.finishAssistantMessage(event.messageId);
        break;
    }
  };

  const send = rxMethod<string>(
    pipe(
      filter(() => store.canSend()),
      tap(text => {
        store.setSuccess();
        store.appendUserMessage(text);
        store.beginAssistantMessage();
        store.setStreaming(true);
      }),
      switchMap(text =>
        agentService.streamChat({conversationId: store.activeConversationId(), message: text}).pipe(
          tap(event => handleEvent(event)),
          catchError((err: unknown) => {
            store.setError(extractErrorCode(err));
            store.endStreamingOnError();
            return EMPTY;
          }),
          finalize(() => {
            store.setStreaming(false);
            loadConversations();
          })
        )
      )
    )
  );

  const selectConversation = rxMethod<string>(
    pipe(
      tap(() => store.setLoading()),
      switchMap(id =>
        agentService.getConversation(id).pipe(
          tap(detail => {
            store.setActiveConversationId(detail.id);
            store.loadHistory(detail.messages);
            store.setSuccess();
          }),
          catchError((err: unknown) => {
            store.setError(extractErrorCode(err));
            return EMPTY;
          })
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
          catchError((err: unknown) => {
            store.setError(extractErrorCode(err));
            return EMPTY;
          })
        )
      )
    )
  );

  return {loadConversations, send, selectConversation, deleteConversation};
}

export function agentChatHooks(store: {loadConversations: () => void}) {
  store.loadConversations();
}

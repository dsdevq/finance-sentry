import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type AgentSseEvent} from '../models/chat/chat.model';
import {type ConversationDetail} from '../models/conversation/conversation.model';
import {AgentService} from '../services/agent.service';
import {agentChatEffects} from './agent-chat.effects';

function buildStore(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    activeConversationId: vi.fn().mockReturnValue(null),
    canSend: vi.fn().mockReturnValue(true),
    setConversations: vi.fn(),
    setActiveConversationId: vi.fn(),
    setStreaming: vi.fn(),
    resetThread: vi.fn(),
    loadHistory: vi.fn(),
    appendUserMessage: vi.fn(),
    beginAssistantMessage: vi.fn(),
    appendAssistantDelta: vi.fn(),
    setToolActivity: vi.fn(),
    finishAssistantMessage: vi.fn(),
    endStreamingOnError: vi.fn(),
    removeConversationLocally: vi.fn(),
    setError: vi.fn(),
    setLoading: vi.fn(),
    setSuccess: vi.fn(),
    ...overrides,
  };
}

function buildService() {
  return {
    listConversations: vi.fn().mockReturnValue(of([])),
    getConversation: vi.fn(),
    deleteConversation: vi.fn().mockReturnValue(of(undefined)),
    streamChat: vi.fn(),
  };
}

function configure(service: ReturnType<typeof buildService>): void {
  TestBed.configureTestingModule({
    providers: [{provide: AgentService, useValue: service}],
  });
}

describe('agentChatEffects', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('send: streams deltas + tool events, then finishes and reloads', () => {
    const store = buildStore();
    const service = buildService();
    const events: AgentSseEvent[] = [
      {type: 'conversation', conversationId: 'c1'},
      {type: 'text', delta: 'Hel'},
      {type: 'text', delta: 'lo'},
      {type: 'tool', name: 'get_ips', phase: 'start'},
      {type: 'tool', name: 'get_ips', phase: 'end'},
      {type: 'done', messageId: 'm1'},
    ];
    service.streamChat.mockReturnValue(of(...events));
    configure(service);

    TestBed.runInInjectionContext(() => agentChatEffects(store).send('hi'));

    expect(store.appendUserMessage).toHaveBeenCalledWith('hi');
    expect(store.beginAssistantMessage).toHaveBeenCalledOnce();
    expect(store.setActiveConversationId).toHaveBeenCalledWith('c1');
    expect(store.appendAssistantDelta).toHaveBeenNthCalledWith(1, 'Hel');
    expect(store.appendAssistantDelta).toHaveBeenNthCalledWith(2, 'lo');
    expect(store.setToolActivity).toHaveBeenCalledWith('get_ips', 'start');
    expect(store.setToolActivity).toHaveBeenCalledWith('get_ips', 'end');
    expect(store.finishAssistantMessage).toHaveBeenCalledWith('m1');
    expect(store.setStreaming).toHaveBeenCalledWith(true);
    expect(store.setStreaming).toHaveBeenLastCalledWith(false);
    expect(service.listConversations).toHaveBeenCalled();
  });

  it('send: maps an error event to setError and stops streaming', () => {
    const store = buildStore();
    const service = buildService();
    service.streamChat.mockReturnValue(
      of<AgentSseEvent>({type: 'error', code: 'agent_not_configured', message: 'nope'})
    );
    configure(service);

    TestBed.runInInjectionContext(() => agentChatEffects(store).send('hi'));

    expect(store.setError).toHaveBeenCalledWith('agent_not_configured');
    expect(store.endStreamingOnError).toHaveBeenCalledOnce();
    expect(store.setStreaming).toHaveBeenLastCalledWith(false);
  });

  it('send: maps a transport failure to an error code', () => {
    const store = buildStore();
    const service = buildService();
    service.streamChat.mockReturnValue(
      throwError(() => new HttpErrorResponse({error: {errorCode: 'llm_unavailable'}, status: 500}))
    );
    configure(service);

    TestBed.runInInjectionContext(() => agentChatEffects(store).send('hi'));

    expect(store.setError).toHaveBeenCalledWith('llm_unavailable');
    expect(store.endStreamingOnError).toHaveBeenCalled();
  });

  it('send: does nothing while already streaming', () => {
    const store = buildStore({canSend: vi.fn().mockReturnValue(false)});
    const service = buildService();
    configure(service);

    TestBed.runInInjectionContext(() => agentChatEffects(store).send('hi'));

    expect(store.appendUserMessage).not.toHaveBeenCalled();
    expect(service.streamChat).not.toHaveBeenCalled();
  });

  it('selectConversation: loads history for the chosen thread', () => {
    const store = buildStore();
    const service = buildService();
    const detail: ConversationDetail = {
      id: 'c1',
      title: 't',
      createdAt: '',
      updatedAt: '',
      modelId: 'm',
      messages: [
        {
          id: 'm1',
          role: 'user',
          content: 'hi',
          toolCallsJson: null,
          toolResultsJson: null,
          createdAt: '',
        },
      ],
    };
    service.getConversation.mockReturnValue(of(detail));
    configure(service);

    TestBed.runInInjectionContext(() => agentChatEffects(store).selectConversation('c1'));

    expect(store.setActiveConversationId).toHaveBeenCalledWith('c1');
    expect(store.loadHistory).toHaveBeenCalledWith(detail.messages);
  });

  it('deleteConversation: removes locally and reloads', () => {
    const store = buildStore();
    const service = buildService();
    configure(service);

    TestBed.runInInjectionContext(() => agentChatEffects(store).deleteConversation('c1'));

    expect(store.removeConversationLocally).toHaveBeenCalledWith('c1');
    expect(service.listConversations).toHaveBeenCalled();
  });
});

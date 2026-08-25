import {TestBed} from '@angular/core/testing';
import {type CmnChatStreamEvent} from '@lifekit-hq/ui';
import {of} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type AgentSseEvent} from '../models/chat/chat.model';
import {type ConversationDetail} from '../models/conversation/conversation.model';
import {AgentService} from '../services/agent.service';
import {agentChatEffects} from './agent-chat.effects';

function buildStore(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    activeConversationId: vi.fn().mockReturnValue(null),
    setConversations: vi.fn(),
    setActiveConversationId: vi.fn(),
    openConversation: vi.fn(),
    removeConversationLocally: vi.fn(),
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

  it('stream: maps text deltas, captures the new conversation id, and reloads on completion', () => {
    const store = buildStore();
    const service = buildService();
    const events: AgentSseEvent[] = [
      {type: 'conversation', conversationId: 'c1'},
      {type: 'text', delta: 'Hel'},
      {type: 'text', delta: 'lo'},
      {type: 'tool', name: 'get_ips', phase: 'start'},
      {type: 'done', messageId: 'm1'},
    ];
    service.streamChat.mockReturnValue(of(...events));
    configure(service);

    const emitted: CmnChatStreamEvent[] = [];
    TestBed.runInInjectionContext(() =>
      agentChatEffects(store)
        .stream('hi')
        .subscribe(e => emitted.push(e))
    );

    expect(service.streamChat).toHaveBeenCalledWith({conversationId: null, message: 'hi'});
    expect(store.setActiveConversationId).toHaveBeenCalledWith('c1');
    // Only text/error events surface to <cmn-chat>; conversation/tool/done are filtered out.
    expect(emitted).toEqual([
      {type: 'text', delta: 'Hel'},
      {type: 'text', delta: 'lo'},
    ]);
    expect(service.listConversations).toHaveBeenCalled();
  });

  it('stream: maps an error event to a CmnChatStreamEvent error', () => {
    const store = buildStore();
    const service = buildService();
    service.streamChat.mockReturnValue(
      of<AgentSseEvent>({type: 'error', code: 'agent_not_configured', message: 'nope'})
    );
    configure(service);

    const emitted: CmnChatStreamEvent[] = [];
    TestBed.runInInjectionContext(() =>
      agentChatEffects(store)
        .stream('hi')
        .subscribe(e => emitted.push(e))
    );

    expect(emitted).toEqual([{type: 'error', message: 'nope'}]);
  });

  it('selectConversation: loads and maps history for the chosen thread', () => {
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
        {
          id: 'm2',
          role: 'assistant',
          content: 'hey',
          toolCallsJson: null,
          toolResultsJson: null,
          createdAt: '',
        },
        {
          id: 'm3',
          role: 'tool',
          content: 'noise',
          toolCallsJson: null,
          toolResultsJson: null,
          createdAt: '',
        },
      ],
    };
    service.getConversation.mockReturnValue(of(detail));
    configure(service);

    TestBed.runInInjectionContext(() => agentChatEffects(store).selectConversation('c1'));

    expect(store.openConversation).toHaveBeenCalledWith('c1', [
      {role: 'user', text: 'hi'},
      {role: 'ai', text: 'hey'},
    ]);
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

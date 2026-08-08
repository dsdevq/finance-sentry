import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {agentChatMethods} from './agent-chat.methods';
import {initialAgentChatState} from './agent-chat.state';

function build() {
  const store = signalState(initialAgentChatState);
  return {store, methods: agentChatMethods(store)};
}

describe('agentChatMethods', () => {
  it('appends a user message then an assistant placeholder', () => {
    const {store, methods} = build();
    methods.appendUserMessage('why is my book down?');
    methods.beginAssistantMessage();

    const messages = store.messages();
    expect(messages).toHaveLength(2);
    expect(messages[0]).toMatchObject({role: 'user', text: 'why is my book down?'});
    expect(messages[1]).toMatchObject({role: 'assistant', text: '', streaming: true});
  });

  it('concatenates streamed deltas onto the last assistant message', () => {
    const {store, methods} = build();
    methods.beginAssistantMessage();
    methods.appendAssistantDelta('Hel');
    methods.appendAssistantDelta('lo');

    expect(store.messages().at(-1)?.text).toBe('Hello');
  });

  it('tracks tool activity start/end on the assistant message', () => {
    const {store, methods} = build();
    methods.beginAssistantMessage();
    methods.setToolActivity('get_ips', 'start');
    expect(store.messages().at(-1)?.tools).toEqual([{name: 'get_ips', running: true}]);

    methods.setToolActivity('get_ips', 'end');
    expect(store.messages().at(-1)?.tools).toEqual([{name: 'get_ips', running: false}]);
  });

  it('finishes the assistant message with the persisted id', () => {
    const {store, methods} = build();
    methods.beginAssistantMessage();
    methods.appendAssistantDelta('done');
    methods.finishAssistantMessage('msg-1');

    const last = store.messages().at(-1);
    expect(last).toMatchObject({id: 'msg-1', streaming: false});
  });

  it('drops an empty assistant placeholder on error', () => {
    const {store, methods} = build();
    methods.appendUserMessage('hi');
    methods.beginAssistantMessage();
    methods.endStreamingOnError();

    const messages = store.messages();
    expect(messages).toHaveLength(1);
    expect(messages[0].role).toBe('user');
  });

  it('keeps a partially streamed message on error but stops streaming', () => {
    const {store, methods} = build();
    methods.beginAssistantMessage();
    methods.appendAssistantDelta('partial');
    methods.endStreamingOnError();

    expect(store.messages().at(-1)).toMatchObject({text: 'partial', streaming: false});
  });

  it('removes a conversation and clears the thread when it was active', () => {
    const {store, methods} = build();
    methods.setConversations([
      {id: 'c1', title: 'a', updatedAt: '', modelId: 'm'},
      {id: 'c2', title: 'b', updatedAt: '', modelId: 'm'},
    ]);
    methods.setActiveConversationId('c1');
    methods.appendUserMessage('hello');

    methods.removeConversationLocally('c1');

    expect(store.conversations().map(c => c.id)).toEqual(['c2']);
    expect(store.activeConversationId()).toBeNull();
    expect(store.messages()).toHaveLength(0);
  });
});

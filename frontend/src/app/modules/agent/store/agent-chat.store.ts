import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {agentChatComputed} from './agent-chat.computed';
import {agentChatEffects, agentChatHooks} from './agent-chat.effects';
import {agentChatMethods} from './agent-chat.methods';
import {initialAgentChatState} from './agent-chat.state';

export const AgentChatStore = signalStore(
  withState(initialAgentChatState),
  withMethods(agentChatMethods),
  withComputed(agentChatComputed),
  withMethods(agentChatEffects),
  withHooks({onInit: agentChatHooks})
);

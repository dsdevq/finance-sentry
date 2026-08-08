import {type ChatToolActivity} from '@dsdevq-common/ui';

/** Payload sent to POST /agent/chat. */
export interface SendChatRequest {
  conversationId: string | null;
  message: string;
}

/** Typed SSE events streamed back from the chat endpoint. */
export type AgentSseEvent =
  | {type: 'conversation'; conversationId: string}
  | {type: 'text'; delta: string}
  | {type: 'tool'; name: string; phase: 'start' | 'end'}
  | {type: 'error'; code: string; message: string}
  | {type: 'done'; messageId: string};

/** A message as rendered in the chat log. */
export interface ChatMessageView {
  id: string;
  role: 'user' | 'assistant';
  text: string;
  streaming: boolean;
  tools: ChatToolActivity[];
}

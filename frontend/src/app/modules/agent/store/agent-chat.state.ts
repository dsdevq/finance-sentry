import {type ChatMessageView} from '../models/chat/chat.model';
import {type ConversationSummary} from '../models/conversation/conversation.model';

export interface AgentChatState {
  conversations: ConversationSummary[];
  activeConversationId: string | null;
  messages: ChatMessageView[];
  isStreaming: boolean;
}

export const initialAgentChatState: AgentChatState = {
  conversations: [],
  activeConversationId: null,
  messages: [],
  isStreaming: false,
};

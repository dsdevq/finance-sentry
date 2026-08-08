export type AgentMessageRole = 'user' | 'assistant' | 'tool';

export interface AgentMessage {
  id: string;
  role: AgentMessageRole;
  content: string;
  toolCallsJson: string | null;
  toolResultsJson: string | null;
  createdAt: string;
}

export interface ConversationSummary {
  id: string;
  title: string | null;
  updatedAt: string;
  modelId: string;
}

export interface ConversationDetail {
  id: string;
  title: string | null;
  createdAt: string;
  updatedAt: string;
  modelId: string;
  messages: AgentMessage[];
}

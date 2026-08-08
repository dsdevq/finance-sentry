import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {Observable} from 'rxjs';

import {type AgentSseEvent, type SendChatRequest} from '../models/chat/chat.model';
import {
  type ConversationDetail,
  type ConversationSummary,
} from '../models/conversation/conversation.model';

const SSE_DELIMITER = '\n\n';

@Injectable({providedIn: 'root'})
export class AgentService extends ApiService {
  constructor() {
    super('agent');
  }

  private static str(value: unknown, fallback = ''): string {
    return typeof value === 'string' ? value : fallback;
  }

  private static parseFrame(frame: string): AgentSseEvent | null {
    let eventName = 'message';
    const dataLines: string[] = [];

    for (const line of frame.split('\n')) {
      if (line.startsWith('event:')) {
        eventName = line.slice('event:'.length).trim();
      } else if (line.startsWith('data:')) {
        dataLines.push(line.slice('data:'.length).trim());
      }
    }

    if (dataLines.length === 0) {
      return null;
    }

    const payload = JSON.parse(dataLines.join('\n')) as Record<string, unknown>;
    return AgentService.toEvent(eventName, payload);
  }

  private static toEvent(name: string, payload: Record<string, unknown>): AgentSseEvent | null {
    switch (name) {
      case 'conversation':
        return {type: 'conversation', conversationId: AgentService.str(payload['conversationId'])};
      case 'text':
        return {type: 'text', delta: AgentService.str(payload['delta'])};
      case 'tool':
        return {
          type: 'tool',
          name: AgentService.str(payload['name']),
          phase: payload['phase'] === 'end' ? 'end' : 'start',
        };
      case 'error':
        return {
          type: 'error',
          code: AgentService.str(payload['code'], 'llm_unavailable'),
          message: AgentService.str(payload['message']),
        };
      case 'done':
        return {type: 'done', messageId: AgentService.str(payload['messageId'])};
      default:
        return null;
    }
  }

  public listConversations(): Observable<ConversationSummary[]> {
    return this.get<ConversationSummary[]>('conversations');
  }

  public getConversation(id: string): Observable<ConversationDetail> {
    return this.get<ConversationDetail>(`conversations/${id}`);
  }

  public deleteConversation(id: string): Observable<void> {
    return this.delete<void>(`conversations/${id}`);
  }

  /**
   * Streams the chat reply over SSE. HttpClient can't surface incremental SSE deltas, so this uses
   * fetch with the auth cookie (credentials: 'include'), parses `event:`/`data:` frames, and emits a
   * typed event per frame. Aborting the subscription cancels the request.
   */
  public streamChat(request: SendChatRequest): Observable<AgentSseEvent> {
    return new Observable<AgentSseEvent>(subscriber => {
      const controller = new AbortController();

      const run = async (): Promise<void> => {
        const response = await fetch(`${this.baseUrl}/chat`, {
          method: 'POST',
          credentials: 'include',
          headers: {'Content-Type': 'application/json', accept: 'text/event-stream'},
          body: JSON.stringify(request),
          signal: controller.signal,
        });

        if (!response.ok || response.body === null) {
          subscriber.error(new Error(`Chat request failed with status ${response.status}.`));
          return;
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        for (;;) {
          const {done, value} = await reader.read();
          if (done) {
            break;
          }

          buffer += decoder.decode(value, {stream: true});
          let boundary = buffer.indexOf(SSE_DELIMITER);
          while (boundary >= 0) {
            const frame = buffer.slice(0, boundary);
            buffer = buffer.slice(boundary + SSE_DELIMITER.length);
            const event = AgentService.parseFrame(frame);
            if (event !== null) {
              subscriber.next(event);
            }

            boundary = buffer.indexOf(SSE_DELIMITER);
          }
        }

        subscriber.complete();
      };

      run().catch((err: unknown) => {
        if (!controller.signal.aborted) {
          subscriber.error(err);
        }
      });

      return () => controller.abort();
    });
  }
}

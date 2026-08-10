import {type Observable} from 'rxjs';

/** A preloaded chat message. Roles are Deep Chat's native `user` / `ai`. */
export interface CmnChatMessage {
  role: 'user' | 'ai';
  text: string;
}

/** One event from an in-flight assistant turn. Stream completion = the turn is done. */
export type CmnChatStreamEvent = {type: 'text'; delta: string} | {type: 'error'; message?: string};

/**
 * Drives one assistant turn: given the user's text, returns a stream of deltas. The host owns the
 * transport (HTTP/SSE) and any conversation-id threading; `cmn-chat` only renders the result.
 */
export type CmnChatStreamFn = (text: string) => Observable<CmnChatStreamEvent>;

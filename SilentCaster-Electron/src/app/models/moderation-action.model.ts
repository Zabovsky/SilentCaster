export enum ModerationActionType {
  DELETE = 'delete',
  TIMEOUT = 'timeout',
  BAN = 'ban',
  UNBAN = 'unban',
  CLEAR = 'clear',
  WHISPER = 'whisper'
}

export interface ModerationAction {
  type: ModerationActionType;
  username: string;
  duration?: number; // seconds for timeout
  reason?: string;
  messageId?: string;
}

export interface ModerationResult {
  success: boolean;
  message: string;
  error?: string;
}


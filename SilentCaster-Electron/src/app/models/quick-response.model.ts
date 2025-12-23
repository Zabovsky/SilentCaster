export interface QuickResponse {
  trigger: string;
  response: string;
  responses: string[];
  category: string;
  priority: number;
  isEnabled: boolean;
  useForChatMessages: boolean;
  useForManualMessages: boolean;
  useForQuickResponses: boolean;
  isQuickResponse: boolean;
  usageChance: number; // 0 to 100
  delay: number; // seconds
}

export class QuickResponseImpl implements QuickResponse {
  trigger: string = '';
  response: string = '';
  responses: string[] = [];
  category: string = 'Общие';
  priority: number = 1;
  isEnabled: boolean = true;
  useForChatMessages: boolean = true;
  useForManualMessages: boolean = true;
  useForQuickResponses: boolean = true;
  isQuickResponse: boolean = false;
  usageChance: number = 100;
  delay: number = 0;
}


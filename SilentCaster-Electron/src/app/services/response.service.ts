import { Injectable } from '@angular/core';
import { QuickResponse, QuickResponseImpl } from '../models/quick-response.model';

@Injectable({
  providedIn: 'root'
})
export class ResponseService {
  private responses: QuickResponse[] = [];
  private readonly RESPONSES_KEY = 'quick_responses';

  constructor() {
    this.loadResponses();
  }

  getAllResponses(): QuickResponse[] {
    return [...this.responses];
  }

  getResponsesForMessage(message: string): string[] {
    const responses: string[] = [];

    // Ищем совпадения по триггерам
    for (const response of this.responses) {
      if (!response.isEnabled) continue;
      
      if (response.trigger === '*' || 
          message.toLowerCase().includes(response.trigger.toLowerCase())) {
        responses.push(...response.responses);
      }
    }

    // Удаляем дубликаты
    return [...new Set(responses)];
  }

  getPersonalResponsesForMessage(message: string): string[] {
    const responses: string[] = [];

    // Ищем только персональные ответы (isQuickResponse == false)
    for (const response of this.responses) {
      if (!response.isEnabled || response.isQuickResponse) continue;
      
      if (response.trigger === '*' || 
          message.toLowerCase().includes(response.trigger.toLowerCase())) {
        responses.push(...response.responses);
      }
    }

    return [...new Set(responses)];
  }

  addResponse(response: QuickResponse): void {
    this.responses.push(response);
    this.saveResponses();
  }

  removeResponse(index: number): void {
    if (index >= 0 && index < this.responses.length) {
      this.responses.splice(index, 1);
      this.saveResponses();
    }
  }

  updateResponse(index: number, response: QuickResponse): void {
    if (index >= 0 && index < this.responses.length) {
      this.responses[index] = response;
      this.saveResponses();
    }
  }

  clearResponses(): void {
    this.responses = [];
    this.saveResponses();
  }

  getQuickResponses(): QuickResponse[] {
    return this.responses.filter(r => r.isQuickResponse);
  }

  getPersonalResponses(): QuickResponse[] {
    return this.responses.filter(r => !r.isQuickResponse);
  }

  updateAllResponses(newResponses: QuickResponse[]): void {
    this.responses = [...newResponses];
    this.saveResponses();
  }

  private loadResponses(): void {
    try {
      if (window.electronAPI && (window.electronAPI as any).loadResponses) {
        const saved = (window.electronAPI as any).loadResponses();
        if (saved && Array.isArray(saved)) {
          this.responses = saved;
          return;
        }
      }

      // Fallback to localStorage
      const saved = localStorage.getItem(this.RESPONSES_KEY);
      if (saved) {
        this.responses = JSON.parse(saved);
      } else {
        // Создаем примеры
        this.responses = [
          { ...new QuickResponseImpl(), trigger: 'привет', responses: ['Привет всем!', 'Приветики!'], isQuickResponse: true },
          { ...new QuickResponseImpl(), trigger: 'спасибо', responses: ['Пожалуйста!', 'Рад помочь!'], isQuickResponse: true },
          { ...new QuickResponseImpl(), trigger: 'follow', responses: ['Спасибо за фоллоу, {username}!'], isQuickResponse: false },
          { ...new QuickResponseImpl(), trigger: 'привет, {username}', responses: ['Привет, {username}!'], isQuickResponse: false }
        ];
        this.saveResponses();
      }
    } catch (error) {
      console.error('Error loading responses:', error);
      this.responses = [];
    }
  }

  private saveResponses(): void {
    try {
      if (window.electronAPI && (window.electronAPI as any).saveResponses) {
        (window.electronAPI as any).saveResponses(this.responses);
        return;
      }

      // Fallback to localStorage
      localStorage.setItem(this.RESPONSES_KEY, JSON.stringify(this.responses));
    } catch (error) {
      console.error('Error saving responses:', error);
    }
  }
}


import { Component, OnInit } from '@angular/core';
import { ForbiddenWordsService } from '../../services/forbidden-words.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-forbidden-words',
  templateUrl: './forbidden-words.component.html',
  styleUrls: ['./forbidden-words.component.scss']
})
export class ForbiddenWordsComponent implements OnInit {
  forbiddenWords$: Observable<string[]>;
  caseSensitive$: Observable<boolean>;
  enabled$: Observable<boolean>;
  
  newWord: string = '';
  caseSensitive: boolean = false;
  enabled: boolean = true;

  constructor(private forbiddenWordsService: ForbiddenWordsService) {
    this.forbiddenWords$ = this.forbiddenWordsService.forbiddenWords$;
    this.caseSensitive$ = this.forbiddenWordsService.caseSensitive$;
    this.enabled$ = this.forbiddenWordsService.enabled$;
  }

  ngOnInit(): void {
    this.caseSensitive$.subscribe(value => this.caseSensitive = value);
    this.enabled$.subscribe(value => this.enabled = value);
  }

  async addWord(): Promise<void> {
    if (this.newWord.trim()) {
      await this.forbiddenWordsService.addForbiddenWord(this.newWord.trim());
      this.newWord = '';
    }
  }

  async removeWord(word: string): Promise<void> {
    await this.forbiddenWordsService.removeForbiddenWord(word);
  }

  async toggleCaseSensitive(): Promise<void> {
    await this.forbiddenWordsService.setCaseSensitive(!this.caseSensitive);
  }

  async toggleEnabled(): Promise<void> {
    await this.forbiddenWordsService.setEnabled(!this.enabled);
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.addWord();
    }
  }
}


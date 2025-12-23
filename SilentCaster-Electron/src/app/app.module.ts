import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AppComponent } from './app.component';
import { MainComponent } from './pages/main/main.component';
import { ChatMessageComponent } from './components/chat-message/chat-message.component';
import { NotificationComponent } from './components/notification/notification.component';
import { ForbiddenWordsComponent } from './components/forbidden-words/forbidden-words.component';
import { UserProfileComponent } from './components/user-profile/user-profile.component';
import { TwitchAuthComponent } from './components/twitch-auth/twitch-auth.component';
import { SettingsComponent } from './components/settings/settings.component';

@NgModule({
  declarations: [
    AppComponent,
    MainComponent,
    ChatMessageComponent,
    NotificationComponent,
    ForbiddenWordsComponent,
    UserProfileComponent,
    TwitchAuthComponent,
    SettingsComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    BrowserAnimationsModule,
    FormsModule,
    ReactiveFormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }


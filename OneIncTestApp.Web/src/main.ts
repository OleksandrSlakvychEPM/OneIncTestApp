import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { App } from './app/app';

bootstrapApplication(App, {
  providers: [
    provideHttpClient(),
    BrowserAnimationsModule, // Required for Angular Material animations
  ],
}).catch((err) => console.error(err));
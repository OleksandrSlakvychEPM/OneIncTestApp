import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatToolbarModule } from '@angular/material/toolbar';
import { CommonModule } from '@angular/common';
import { ProcessingService } from './services/processing.service';
import { Subscription } from 'rxjs';


@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatToolbarModule,
    MatProgressBarModule
  ],
  providers: [ProcessingService], // Provide the service here
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit, OnDestroy {
  processingForm!: FormGroup;
  isProcessing = false;
  output = '';
  totalCharacters = 0;
  progress = 0;

  // Subscriptions for observables
  private characterReceivedSubscription!: Subscription;
  private processingCompleteSubscription!: Subscription;
  private processingOutputLengthSubscription!: Subscription;

  // Use inject() for dependency injection
  private fb = inject(FormBuilder);
  private processingService = inject(ProcessingService);

  ngOnInit(): void {
    // Initialize the SignalR connection
    this.processingService.startConnection();

    // Initialize the form with validation
    this.processingForm = this.fb.group({
      inputText: ['', [Validators.required, Validators.minLength(1)]]
    });

    this.characterReceivedSubscription = this.processingService.getCharacterReceivedObservable().subscribe({
      next: (char: string) => {
        if (this.isProcessing) {
          this.output += char; // Append received character to output

          // Update progress
          const receivedCharacters = this.output.length;
          this.progress = (receivedCharacters / this.totalCharacters) * 100;
        }
      },
      error: (err) => console.error('Error receiving character:', err)
    });

    this.processingCompleteSubscription = this.processingService.getProcessingCompleteObservable().subscribe({
      next: () => {
        this.isProcessing = false; // Processing complete
      },
      error: (err) => console.error('Error during processing completion:', err)
    });

    this.processingOutputLengthSubscription = this.processingService.getProcessingOutputLengthObservable().subscribe({
      next: (length: number) => {
        this.totalCharacters = length;
      },
      error: (err) => console.error('Error during processing completion:', err)
    });
  }

  startProcessing(): void {
    if (this.processingForm.invalid) {
      return;
    }

    const inputText = this.processingForm.value.inputText;
    this.isProcessing = true;
    this.output = '';
    this.progress = 0;

    // Start processing via the service
    this.processingService.processText(inputText);
  }

  cancelProcessing(): void {
    this.isProcessing = false;
    this.progress = 0; // Reset progress
    this.processingService.cancelProcessing(); // Call the service to cancel processing
  }

  ngOnDestroy(): void {
    if (this.characterReceivedSubscription) {
      this.characterReceivedSubscription.unsubscribe();
    }
    if (this.processingCompleteSubscription) {
      this.processingCompleteSubscription.unsubscribe();
    }
    if (this.processingOutputLengthSubscription) {
      this.processingOutputLengthSubscription.unsubscribe();
    }
  }
}
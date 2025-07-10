import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class ProcessingService {
    private hubConnection!: signalR.HubConnection;
    private http = inject(HttpClient); // Use inject() for HttpClient
    private apiUrl = 'https://localhost:7122/api/processing'; // Base API URL

    private characterReceivedSubject = new Subject<string>();
    private processingCompleteSubject = new Subject<void>();
    private processingOutputLengthSubject = new Subject<number>();

    totalCharacters = 0; // Total characters for progress calculation

    startConnection(): void {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl('https://localhost:7122/processingHub') // SignalR Hub URL
            .build();

        this.hubConnection
            .start()
            .then(() => {
                console.log('SignalR connected:', this.hubConnection.connectionId);

                // Register event listeners once after connection is established
                this.registerEventListeners();
            })
            .catch((err) => console.error('Error while starting SignalR connection:', err));
    }

    // Register SignalR event listeners
    private registerEventListeners(): void {
        // Listen for real-time updates
        this.hubConnection.on('ProcessingOutputLength', (length: number) => {
            console.log('Processing Output Length:', length); // Debug log
            this.processingOutputLengthSubject.next(length);
        });

        this.hubConnection.on('ReceiveCharacter', (char: string) => {
            console.log('Received a character:', char); // Debug log
            this.characterReceivedSubject.next(char); // Push the character to the observable
        });

        this.hubConnection.on('ProcessingComplete', () => {
            console.log('Processing complete'); // Debug log
            this.processingCompleteSubject.next(); // Notify subscribers that processing is complete
        });

        this.hubConnection.on('ProcessingCancelled', () => {
            console.log('Processing cancelled'); // Debug log
            this.processingCompleteSubject.next(); // Notify subscribers about cancellation
        });
    }

    processText(input: string): void {
        if (!this.hubConnection.connectionId) {
            console.error('SignalR connection is not established.');
            return;
        }

        // Prepare the request payload
        const request = {
            input: input,
            connectionId: this.hubConnection.connectionId, // SignalR connection ID
        };

        // Call the API endpoint to start processing
        this.http.post(`${this.apiUrl}/start`, request).subscribe({
            next: (response) => {
                console.log('Processing started:', response);
            },
            error: (err) => {
                console.error('Error while starting processing:', err);
            },
        });
    }

    cancelProcessing(): void {
        this.hubConnection.invoke('CancelProcessing').catch((err) => console.error('Error while canceling processing:', err));
    }

    getCharacterReceivedObservable(): Observable<string> {
        return this.characterReceivedSubject.asObservable();
    }

    getProcessingCompleteObservable(): Observable<void> {
        return this.processingCompleteSubject.asObservable();
    }

    getProcessingOutputLengthObservable(): Observable<number> {
        return this.processingOutputLengthSubject.asObservable();
    }
}
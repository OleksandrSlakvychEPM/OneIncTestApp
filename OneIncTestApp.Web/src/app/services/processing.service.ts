import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class ProcessingService {
    private hubConnection!: signalR.HubConnection;
    private http = inject(HttpClient);
    private apiUrl = 'http://localhost:8080/api/processing';
    private hubUrl = 'http://localhost:8080/api/processingHub';

    private characterReceivedSubject = new Subject<string>();
    private processingCompleteSubject = new Subject<void>();
    private processingOutputLengthSubject = new Subject<number>();

    totalCharacters = 0; // Total characters for progress calculation

    startConnection(): void {

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .build();

        this.hubConnection
            .start()
            .then(() => {
                console.log('SignalR connected:', this.hubConnection.connectionId);

                this.registerEventListeners();
            })
            .catch((err) => console.error('Error while starting SignalR connection:', err));
    }

    private registerEventListeners(): void {
        this.hubConnection.on('ProcessingOutputLength', (length: number) => {
            console.log('Processing Output Length:', length);
            this.processingOutputLengthSubject.next(length);
        });

        this.hubConnection.on('ReceiveCharacter', (char: string) => {
            console.log('Received a character:', char);
            this.characterReceivedSubject.next(char);
        });

        this.hubConnection.on('ProcessingComplete', () => {
            console.log('Processing complete');
            this.processingCompleteSubject.next();
        });

        this.hubConnection.on('ProcessingCancelled', () => {
            console.log('Processing cancelled');
            this.processingCompleteSubject.next();
        });
    }

    processText(input: string): void {
        if (!this.hubConnection.connectionId) {
            console.error('SignalR connection is not established.');
            return;
        }

        const request = {
            input: input,
            connectionId: this.hubConnection.connectionId,
        };

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
        if (!this.hubConnection.connectionId) {
            console.error('SignalR connection is not established.');
            return;
        }

        const connectionId = this.hubConnection.connectionId;

        this.http.post(`${this.apiUrl}/cancel`, { connectionId }).subscribe({
            next: () => {
                console.log('Processing cancelled successfully.');
            },
            error: (err) => {
                console.error('Error while canceling processing:', err);
            },
        });
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
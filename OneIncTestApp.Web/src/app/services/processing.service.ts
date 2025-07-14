import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { TabIdentifierService } from './tabidentifier.service';

@Injectable({
    providedIn: 'root',
})
export class ProcessingService {
    private hubConnection!: signalR.HubConnection;
    private http = inject(HttpClient);
    private apiUrl = 'http://localhost:8080/api/processing';
    private hubUrl = 'http://localhost:8080/api/processingHub';

    private tabId: string;

    private characterReceivedSubject = new Subject<string>();
    private processingCompleteSubject = new Subject<void>();
    private processingOutputLengthSubject = new Subject<number>();

    totalCharacters = 0; // Total characters for progress calculation

    constructor(private tabIdentifierService: TabIdentifierService) {
        this.tabId = this.tabIdentifierService.getTabId();
    }

    startConnection(retries: number = 3, delayMs: number = 2000): void {

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .withAutomaticReconnect()
            .build();

        this.hubConnection
            .start()
            .then(() => {
                console.log('SignalR connected:', this.hubConnection.connectionId);
                this.registerEventListeners();
            })
            .catch((err) => {
                console.error('Error while starting SignalR connection:', err);
                if (retries > 0) {
                    setTimeout(() => this.startConnection(retries - 1, delayMs), delayMs);
                }
            });
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
            tabId: this.tabId,
        };

        this.http.post(`${this.apiUrl}/start`, request).subscribe({
            next: (response) => {
                console.log('Processing started:', response);
            },
            error: (err) => {
                console.error('Error while starting processing:', err);
                this.characterReceivedSubject.error(err);
            },
        });
    }

    cancelProcessing(): void {
        if (!this.hubConnection.connectionId) {
            console.error('SignalR connection is not established.');
            return;
        }

        const request = {
            connectionId: this.hubConnection.connectionId,
            tabId: this.tabId,
        };

        this.http.post(`${this.apiUrl}/cancel`, request).subscribe({
            next: () => {
                console.log('Processing cancelled successfully.');
            },
            error: (err) => {
                console.error('Error while canceling processing:', err);
            },
        });
    }

    stopConnection(): void {
        if (this.hubConnection) {
            this.hubConnection.stop()
                .then(() => console.log('SignalR connection stopped'))
                .catch((err) => console.error('Error while stopping SignalR connection:', err));
        }
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
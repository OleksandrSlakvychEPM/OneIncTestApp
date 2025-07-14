import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class TabIdentifierService {
    private readonly tabIdKey = 'tabId';
    private tabId: string;

    constructor() {
        const existingTabId = sessionStorage.getItem(this.tabIdKey);
        if (existingTabId) {
            this.tabId = existingTabId;
        } else {
            this.tabId = this.generateUniqueId();
            sessionStorage.setItem(this.tabIdKey, this.tabId);
        }
    }

    getTabId(): string {
        return this.tabId;
    }

    private generateUniqueId(): string {
        return 'tab-' + Math.random().toString(36).substr(2, 9);
    }
}
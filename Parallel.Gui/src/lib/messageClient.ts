import * as signalR from "@microsoft/signalr";

export class MessageClient {
    private connectionChangedCallbacks: ((isConnected: boolean) => void)[] = [];
    private connection: signalR.HubConnection;

    constructor(url: string) {
        this.connection = new signalR.HubConnectionBuilder().withUrl(url, { withCredentials: true }).withAutomaticReconnect().build();
    }

    isConnected() {
        return this.connection.state === signalR.HubConnectionState.Connected;
    }

    async connect() {
        if (this.connection.state != signalR.HubConnectionState.Disconnected) return;
        await this.connection.start();

        // Notify all registered callbacks.
        this.connectionChangedCallbacks.forEach(cb => cb(this.isConnected()));
    }

    async disconnect() {
        if (this.connection.state === signalR.HubConnectionState.Disconnected) return;
        await this.connection.stop();
    }

    // Wrapper for .on() — callback receives a string
    on(eventName: string, callback: (message: string) => void) {
        this.connection.on(eventName, callback);
    }

    off(eventName: string, callback?: (message: string) => void) {
        if (callback) this.connection.off(eventName, callback);
        else this.connection.off(eventName); // remove all listeners for that event
    }

    connectionChanged = (callback: (isConnected: boolean) => void) => {
        this.connectionChangedCallbacks.push(callback);
        callback(this.isConnected());

        this.connection.onreconnected(() => callback(this.isConnected()));
        this.connection.onclose(() => callback(this.isConnected()));
    }

    // Optional: helper for sending messages
    async send(eventName: string, message: string) {
        await this.connect();
        await this.connection.invoke(eventName, message);
    }
}

export const client = new MessageClient("http://127.0.0.1:5000/hub");
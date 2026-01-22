export {};

declare global {
    interface Window {
        electronAPI: {
            sayHello: (name: string) => Promise<string>;
            // add other IPC methods here later if needed
        };
    }
}
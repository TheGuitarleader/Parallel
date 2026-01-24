export {};

declare global {
    interface Window {
        electronAPI: {
            getMachineName(): Promise<string>;
            minimize: () => void;
            maximize: () => void;
            close: () => void;
        };
    }
}
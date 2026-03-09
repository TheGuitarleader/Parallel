export function formatBytes(bytes: number): string {
    if (bytes === 0) return "0 B";

    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    const value = (bytes / Math.pow(k, i))

    return `${isNaN(value) ? '0 Bytes' : `${value.toFixed(2)} ${sizes[i]}`}`;
}

export function formatNum(num: number): string {
    return num.toLocaleString();
}
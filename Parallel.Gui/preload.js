const { contextBridge, ipcRenderer } = require('electron');

console.log("Preloaded!")

contextBridge.exposeInMainWorld('electronAPI', {
    getMachineName: () => ipcRenderer.invoke("hostname"),
    minimize: () => ipcRenderer.send("window:minimize"),
    maximize: () => ipcRenderer.send("window:maximize"),
    close: () => ipcRenderer.send("window:close")
});
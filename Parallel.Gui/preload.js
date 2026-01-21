const { contextBridge, ipcRenderer } = require('electron');

console.log("Preload loaded!");

contextBridge.exposeInMainWorld('electronAPI', {
  sayHello: (name) => ipcRenderer.invoke('say-hello', name)
});
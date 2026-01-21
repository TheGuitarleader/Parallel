import { app, BrowserWindow, ipcMain } from 'electron'
import path from 'path'
import { fileURLToPath } from 'url'
import grpcClient from './src/lib/grpcClient.js';

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

function createWindow() {
  const win = new BrowserWindow({
    width: 1000,
    height: 800,
    webPreferences: {
        preload: path.join(__dirname, 'preload.js'),
        contextIsolation: true,
        nodeIntegration: false
    }
  })

  if (process.env.ELECTRON_START_URL) {
    // Development mode — use CRA dev server
    win.loadURL(process.env.ELECTRON_START_URL)
  } else {
    // Production mode — load built React files
    win.loadFile(path.join(__dirname, 'build', 'index.html'))
  }

  win.webContents.openDevTools({ mode: 'detach' });
}

app.whenReady().then(createWindow);
app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit()
});

// IPCs
ipcMain.handle('say-hello', async (event, name) => {
    console.log(`Sending gRPC 'say-hello' with name: ${name}`);
    return new Promise((resolve, reject) => {
        grpcClient.SayHello({ name }, (err, response) => {
          if (err) {
            console.error("Error in gRPC call:", err);
            return reject(err);
          }
          resolve(response.message);
        });
    });
});
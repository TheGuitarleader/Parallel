import { app, BrowserWindow, ipcMain } from 'electron'
import path from 'path'
import { fileURLToPath } from 'url'

let win = null;
const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

function createWindow() {
    win = new BrowserWindow({
        width: 1000,
        height: 700,
        frame: false,
        transparent: false,
        icon: path.join(__dirname, 'assets', 'icon.ico'),
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
        win.loadFile(path.join(__dirname, 'dist', 'index.html'))
    }

    win.webContents.openDevTools({ mode: 'detach' });
    win.on("closed", () => { win = null });
}

app.whenReady().then(createWindow);

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
})

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit()
});

// IPCs
ipcMain.on("window:minimize", () => win.minimize())
ipcMain.on("window:maximize", () => {
    if (win.isMaximized()) win.unmaximize()
    else win.maximize()
})
ipcMain.on("window:close", () => win.close())
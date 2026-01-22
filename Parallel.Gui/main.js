import { app, BrowserWindow, ipcMain } from 'electron'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

function createWindow() {
    const win = new BrowserWindow({
        width: 1000,
        height: 700
    })

    if (process.env.ELECTRON_START_URL) {
        // Development mode — use CRA dev server
        win.loadURL(process.env.ELECTRON_START_URL)
    } else {
        // Production mode — load built React files
        win.loadFile(path.join(__dirname, 'dist', 'index.html'))
    }

    win.webContents.openDevTools({ mode: 'detach' });
}

app.whenReady().then(createWindow);

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
})

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit()
});
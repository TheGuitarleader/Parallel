import { app, BrowserWindow } from 'electron'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

function createWindow() {
  const win = new BrowserWindow({
    width: 1000,
    height: 800,
  })

  if (process.env.ELECTRON_START_URL) {
    // Development mode — use CRA dev server
    win.loadURL(process.env.ELECTRON_START_URL)
  } else {
    // Production mode — load built React files
    win.loadFile(path.join(__dirname, 'build', 'index.html'))
  }
}

app.whenReady().then(createWindow);
app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit()
});

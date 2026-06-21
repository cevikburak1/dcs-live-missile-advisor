const { app, BrowserWindow } = require("electron");
const path = require("path");

function createWindow() {
  const win = new BrowserWindow({
    width: 900,
    height: 700,
    transparent: true,
    frame: false,
    alwaysOnTop: true,
    backgroundColor: "#00000000",
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
    },
  });

  const devUrl = process.env.VITE_DEV_URL ?? "http://localhost:5173";
  const prodPath = path.join(__dirname, "..", "frontend", "dist", "index.html");

  if (process.env.ELECTRON_DEV === "1") {
    win.loadURL(devUrl);
  } else {
    win.loadFile(prodPath);
  }
}

app.whenReady().then(createWindow);

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit();
});

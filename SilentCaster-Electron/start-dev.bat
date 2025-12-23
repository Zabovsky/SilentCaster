@echo off
echo Starting Angular dev server...
start "Angular Dev Server" cmd /k "npm start"

echo Waiting for Angular dev server to start...
timeout /t 15 /nobreak

echo Starting Electron...
npm run electron


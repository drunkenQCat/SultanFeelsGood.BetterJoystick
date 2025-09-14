# Launch Sultan's Game and wait for completion
Write-Host "Launching Sultan's Game..." -ForegroundColor Green

# Start the game via Steam
Start-Process steam://rungameid/3117820

Write-Host "Game launched. Waiting 40 seconds for game to complete..." -ForegroundColor Yellow

# Wait 40 seconds for the game to run and complete
Start-Sleep -Seconds 40

Write-Host "Wait completed. Check the log file for any errors." -ForegroundColor Green
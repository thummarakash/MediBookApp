Set-Location $PSScriptRoot
Write-Host "Cleaning old MAUI build files..."
Remove-Item -Recurse -Force bin,obj -ErrorAction SilentlyContinue
Write-Host "Restoring MAUI workloads..."
dotnet workload restore
Write-Host "Restoring NuGet packages for Android target..."
dotnet restore .\MediBook.csproj -p:TargetFramework=net10.0-android
Write-Host "Building Android Debug..."
dotnet build .\MediBook.csproj -f net10.0-android -c Debug

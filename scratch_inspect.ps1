
Add-Type -AssemblyName presentationCore
 = Get-ChildItem 'C:\Users\Duy\.gemini\antigravity\brain\37912308-2749-42bc-973d-154d2b4ce7de\.user_uploaded\uploaded_media_*'
foreach ( in ) {
     = New-Object System.Windows.Media.MediaPlayer
    .Open([System.Uri].FullName)
    Start-Sleep -Milliseconds 400
    Write-Host .Name 'Length:' .NaturalDuration.TimeSpan
}

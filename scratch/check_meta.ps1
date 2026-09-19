
    $shell = New-Object -COMObject Shell.Application
    $folder = $shell.NameSpace('C:\Users\Duy\.gemini\antigravity\brain\37912308-2749-42bc-973d-154d2b4ce7de\.user_uploaded')
    foreach ($file in $folder.Items()) {
        if ($file.Name -like 'uploaded_media_*') {
            $name = $folder.GetDetailsOf($file, 0)
            $type = $folder.GetDetailsOf($file, 2)
            $length = $folder.GetDetailsOf($file, 27) # Audio length/duration
            $bitrate = $folder.GetDetailsOf($file, 28) # Bitrate
            $title = $folder.GetDetailsOf($file, 21) # Title
            Write-Host "$name | Length: $length | Title: $title | Bitrate: $bitrate"
        }
    }
    
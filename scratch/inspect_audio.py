import os
import glob
import subprocess

uploaded_dir = r"C:\Users\Duy\.gemini\antigravity\brain\37912308-2749-42bc-973d-154d2b4ce7de\.user_uploaded"
files = sorted(glob.glob(os.path.join(uploaded_dir, "uploaded_media_*")))

for f in files:
    size = os.path.getsize(f)
    print(f"File: {os.path.basename(f)}, Size: {size} bytes")

# Let's inspect the files using Windows Shell COM object (Shell.Application) which reads audio metadata
try:
    ps_cmd = """
    $shell = New-Object -COMObject Shell.Application
    $folder = $shell.NameSpace('C:\\Users\\Duy\\.gemini\\antigravity\\brain\\37912308-2749-42bc-973d-154d2b4ce7de\\.user_uploaded')
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
    """
    with open("scratch/check_meta.ps1", "w", encoding="utf-8") as ps_file:
        ps_file.write(ps_cmd)
    
    result = subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", "scratch/check_meta.ps1"], capture_output=True, text=True)
    print("Shell metadata:")
    print(result.stdout)
    if result.stderr:
        print("Error:", result.stderr)
except Exception as e:
    print("Error:", e)

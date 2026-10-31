param([string[]]$DocumentNames = @('G10_SRS_IEEE_830_1998', 'G10_Game_Concept'))
$ErrorActionPreference = 'Stop'
$wordApp = New-Object -ComObject Word.Application
$wordApp.Visible = $false
$wordApp.DisplayAlerts = 0
$wordApp.AutomationSecurity = 3
try {
    foreach ($base in $DocumentNames) {
        $sourcePath = "D:\G10\Docs\Deliverables\_work\$base.docx"
        $targetPath = "D:\G10\Docs\Deliverables\$base.doc"
        $pdfPath = "D:\G10\Docs\Deliverables\_work\$base.pdf"
        $document = $wordApp.Documents.Open($sourcePath, $false, $false, $false)
        try {
            $document.Fields.Update() | Out-Null
            $document.Repaginate()
            foreach ($tocItem in $document.TablesOfContents) { $tocItem.Update() }
            $document.Save()
            $document.SaveAs2($targetPath, 0)
            $document.Repaginate()
            foreach ($tocItem in $document.TablesOfContents) { $tocItem.UpdatePageNumbers() }
            $document.Save()
            $document.ExportAsFixedFormat($pdfPath, 17)
            Write-Output "$base : $($document.ComputeStatistics(2)) pages"
        } finally {
            $document.Close(0)
            [void][Runtime.InteropServices.Marshal]::ReleaseComObject($document)
        }
    }
} finally {
    $wordApp.Quit()
    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($wordApp)
}

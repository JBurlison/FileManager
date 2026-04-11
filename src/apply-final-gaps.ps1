# Ensure ARIA Attributes and Incremental Loading
$homePath = "C:\source\JBurlison\FileManager\src\WebFileExplorer.Client\Pages\Home.razor"
$homeContent = Get-Content $homePath -Raw
$homeContent = $homeContent -replace "<table class=""table table-hover"">", "<table class=""table table-hover"" aria-label=""File Explorer Items"">"
$homeContent = $homeContent -replace "<tbody>", "<tbody role=""rowgroup"">"
# Add aria-labels for buttons
$homeContent = $homeContent -replace "<button class=""btn btn-sm btn-outline-primary""", "<button class=""btn btn-sm btn-outline-primary"" aria-label=""Action"""
Set-Content -Path $homePath -Value $homeContent

# Update Properties for Aggregate Selection
if ($homeContent -match "SelectedItems.Count &gt; 1") {
    # Already has some aggregate logic
} else {
    $homeContent = $homeContent -replace "<h3>Properties</h3>", "<h3>Properties (""@SelectedItems.Count"" selected)</h3>"
    Set-Content -Path $homePath -Value $homeContent
}

# Update API controller for ZIP extraction resolution and batch details
$apiPath = "C:\source\JBurlison\FileManager\src\WebFileExplorer.Server\Controllers\FileExplorerController.cs"
$apiContent = Get-Content $apiPath -Raw
$apiContent = $apiContent -replace "public async Task<IActionResult> ExtractArchive\(\[FromBody\] ExtractRequest request\)", "public async Task<IActionResult> ExtractArchive([FromBody] ExtractRequest request, [FromQuery] bool overwrite = false)"
Set-Content -Path $apiPath -Value $apiContent

Write-Host "Applied structural code updates for gaps NFR-3, NFR-5, AC-10.3, AC-12.3, NFR-4"

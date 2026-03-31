param(
    [string]$Configuration = "Debug"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
$coverageOutputRoot = Join-Path $repoRoot "artifacts/coverage"
$htmlReportPath = Join-Path $repoRoot "artifacts/coverage-report"
$testResultsRoot = Join-Path $repoRoot "artifacts/test-results/coverage"

if (Test-Path $testResultsRoot) {
    Remove-Item -Path $testResultsRoot -Recurse -Force
}

Write-Host "Running tests with OpenCover output..."
dotnet test (Join-Path $repoRoot "Fintable.slnx") --configuration $Configuration --results-directory $testResultsRoot --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE."
}

$coverageFiles = @(Get-ChildItem -Path $testResultsRoot -Recurse -Filter "coverage.opencover.xml" | Select-Object -ExpandProperty FullName)
if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
    throw "No coverage.opencover.xml files were generated."
}

New-Item -ItemType Directory -Path $coverageOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $htmlReportPath -Force | Out-Null

# Keep a predictable XML path for local inspection/tooling.
$primaryCoverageFile = Join-Path $coverageOutputRoot "coverage.opencover.xml"
Copy-Item -Path $coverageFiles[0] -Destination $primaryCoverageFile -Force

$reportGeneratorCommand = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
if (-not $reportGeneratorCommand) {
    Write-Host "Installing ReportGenerator global tool..."
    dotnet tool update --global dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        dotnet tool install --global dotnet-reportgenerator-globaltool
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install dotnet-reportgenerator-globaltool."
        }
    }

    $toolsPath = Join-Path $env:USERPROFILE ".dotnet/tools"
    if ($env:PATH -notlike "*$toolsPath*") {
        $env:PATH = "$env:PATH;$toolsPath"
    }
}

$reportsArg = ($coverageFiles -join ";")
reportgenerator "-reports:$reportsArg" "-targetdir:$htmlReportPath" "-reporttypes:Html;HtmlSummary"
if ($LASTEXITCODE -ne 0) {
    throw "reportgenerator failed with exit code $LASTEXITCODE."
}

$indexFile = Join-Path $htmlReportPath "index.html"
if (-not (Test-Path $indexFile)) {
    throw "HTML report was not generated. Expected: $indexFile"
}

Write-Host ""
Write-Host "Coverage completed successfully."
Write-Host "OpenCover XML: $primaryCoverageFile"
Write-Host "HTML report: $indexFile"

param(
    [ValidateSet('Debug','Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipExamples,

    [switch] $SkipPack,

    [switch] $UpdateVisualBaseline
)

function Assert-VisualComparisonHealth {
    param(
        [Parameter(Mandatory = $true)] $Comparison,
        [Parameter(Mandatory = $true)] [string] $ComparisonManifest
    )

    $minimumChartPairs = 50
    if ($Comparison.chartPairs -lt $minimumChartPairs) {
        throw "SVG/PNG comparison generated only $($Comparison.chartPairs) chart pair(s); expected at least $minimumChartPairs. See $ComparisonManifest."
    }

    if ($Comparison.dimensionMatches -ne $Comparison.chartPairs) {
        throw "SVG/PNG comparison has $($Comparison.dimensionMatches) dimension-matched chart pair(s) out of $($Comparison.chartPairs). See $ComparisonManifest."
    }

    if ($Comparison.healthySvgs -ne $Comparison.chartPairs -or $Comparison.healthyPngs -ne $Comparison.chartPairs) {
        throw "SVG/PNG comparison health is incomplete: $($Comparison.healthySvgs) SVG(s), $($Comparison.healthyPngs) PNG(s), $($Comparison.chartPairs) chart pair(s). See $ComparisonManifest."
    }

    if ($Comparison.warnings -ne 0) {
        throw "SVG/PNG comparison reported $($Comparison.warnings) visual warning(s). See $ComparisonManifest."
    }
}

function New-VisualBaseline {
    param(
        [Parameter(Mandatory = $true)] $Comparison
    )

    $updatedCharts = foreach ($chart in $Comparison.charts) {
        [ordered]@{
            name = $chart.name
            width = [int]$chart.svg.width
            height = [int]$chart.svg.height
            svg = [ordered]@{
                minVisualNodes = [int][Math]::Max(2, [int][Math]::Floor([double]$chart.svg.visualNodes * 0.5))
                maxClippedTextNodes = [int]$chart.svg.clippedTextNodes
                maxNearEdgeTextNodes = [int]$chart.svg.nearEdgeTextNodes
            }
            png = [ordered]@{
                outputScale = [int]$chart.png.scale
                minVisiblePixels = [long][Math]::Max(64, [long][Math]::Floor([double]$chart.png.visiblePixels * 0.5))
                minDistinctColors = [int][Math]::Max(8, [int][Math]::Floor([double]$chart.png.distinctColors * 0.5))
                maxEdgeInkPixels = [long]$chart.png.edgeInkPixels
            }
        }
    }

    [ordered]@{
        version = 1
        charts = @($updatedCharts)
    }
}

function Update-VisualBaseline {
    param(
        [Parameter(Mandatory = $true)] $Comparison,
        [Parameter(Mandatory = $true)] [string] $VisualBaselinePath
    )

    New-VisualBaseline -Comparison $Comparison | ConvertTo-Json -Depth 8 | Set-Content -Path $VisualBaselinePath -Encoding UTF8
    Write-Host "Updated SVG/PNG visual baseline: $VisualBaselinePath"
}

function Assert-VisualBaseline {
    param(
        [Parameter(Mandatory = $true)] $Comparison,
        [Parameter(Mandatory = $true)] [string] $VisualBaselinePath,
        [Parameter(Mandatory = $true)] [string] $ComparisonManifest
    )

    if (-not (Test-Path $VisualBaselinePath)) {
        throw "SVG/PNG visual baseline was not found: $VisualBaselinePath"
    }

    $visualBaseline = Get-Content -Path $VisualBaselinePath -Raw | ConvertFrom-Json
    $generatedCharts = @{}
    foreach ($chart in $Comparison.charts) {
        $generatedCharts[$chart.name] = $chart
    }

    $baselineCharts = @{}
    foreach ($expected in $visualBaseline.charts) {
        $baselineCharts[$expected.name] = $expected
        if (-not $generatedCharts.ContainsKey($expected.name)) {
            throw "SVG/PNG baseline chart is missing from generated comparison: $($expected.name). See $ComparisonManifest."
        }

        $actual = $generatedCharts[$expected.name]
        $expectedScale = if ($expected.png.PSObject.Properties.Name -contains 'outputScale') { [int]$expected.png.outputScale } else { 1 }
        if ($actual.svg.width -ne $expected.width -or $actual.svg.height -ne $expected.height -or $actual.png.width -ne ($expected.width * $expectedScale) -or $actual.png.height -ne ($expected.height * $expectedScale)) {
            throw "SVG/PNG baseline dimensions changed for $($expected.name). Expected $($expected.width)x$($expected.height) at PNG scale $expectedScale. See $ComparisonManifest."
        }

        if ($actual.svg.visualNodes -lt $expected.svg.minVisualNodes) {
            throw "SVG visual-node baseline dropped for $($expected.name): $($actual.svg.visualNodes) < $($expected.svg.minVisualNodes). See $ComparisonManifest."
        }

        $maxClippedTextNodes = if ($expected.svg.PSObject.Properties.Name -contains 'maxClippedTextNodes') { [int]$expected.svg.maxClippedTextNodes } else { 0 }
        $maxNearEdgeTextNodes = if ($expected.svg.PSObject.Properties.Name -contains 'maxNearEdgeTextNodes') { [int]$expected.svg.maxNearEdgeTextNodes } else { [int]::MaxValue }
        if ($actual.svg.clippedTextNodes -gt $maxClippedTextNodes -or $actual.svg.nearEdgeTextNodes -gt $maxNearEdgeTextNodes) {
            throw "SVG text-edge baseline regressed for $($expected.name): clipped $($actual.svg.clippedTextNodes), near-edge $($actual.svg.nearEdgeTextNodes). See $ComparisonManifest."
        }

        if ($actual.png.visiblePixels -lt $expected.png.minVisiblePixels -or $actual.png.distinctColors -lt $expected.png.minDistinctColors) {
            throw "PNG visibility baseline dropped for $($expected.name): $($actual.png.visiblePixels) visible pixel(s), $($actual.png.distinctColors) color(s). See $ComparisonManifest."
        }

        $maxEdgeInkPixels = if ($expected.png.PSObject.Properties.Name -contains 'maxEdgeInkPixels') { [long]$expected.png.maxEdgeInkPixels } else { 0 }
        if ($actual.png.edgeInkPixels -gt $maxEdgeInkPixels) {
            throw "PNG edge-pressure baseline regressed for $($expected.name): $($actual.png.edgeInkPixels) edge-ink pixel(s). See $ComparisonManifest."
        }
    }

    foreach ($actual in $Comparison.charts) {
        if (-not $baselineCharts.ContainsKey($actual.name)) {
            throw "SVG/PNG generated chart is missing from visual baseline: $($actual.name). Update $VisualBaselinePath."
        }
    }
}

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSNativeCommandUseErrorActionPreference = $true
}
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    $solution = Join-Path $root 'ChartForgeX.sln'
    $tests = Join-Path $root 'ChartForgeX.Tests/ChartForgeX.Tests.csproj'
    $examples = Join-Path $root 'ChartForgeX.Examples/ChartForgeX.Examples.csproj'
    $library = Join-Path $root 'ChartForgeX/ChartForgeX.csproj'
    if ($SkipExamples -and $UpdateVisualBaseline) {
        throw 'Visual baseline updates require examples to run. Remove -SkipExamples.'
    }

    dotnet restore .\ChartForgeX.sln
    dotnet build $solution -c $Configuration --no-restore
    dotnet test $tests -c $Configuration --no-build --no-restore

    if (-not $SkipExamples) {
        dotnet run --project $examples -c $Configuration --no-build
        $comparisonManifest = Join-Path $root "ChartForgeX.Examples/bin/$Configuration/net8.0/output/svg-png-comparison.json"
        if (-not (Test-Path $comparisonManifest)) {
            throw "SVG/PNG comparison manifest was not generated: $comparisonManifest"
        }

        $comparison = Get-Content -Path $comparisonManifest -Raw | ConvertFrom-Json
        Assert-VisualComparisonHealth -Comparison $comparison -ComparisonManifest $comparisonManifest
        $visualBaselinePath = Join-Path $root "ChartForgeX.Examples/visual-baseline.json"
        if ($UpdateVisualBaseline) {
            Update-VisualBaseline -Comparison $comparison -VisualBaselinePath $visualBaselinePath
        }

        Assert-VisualBaseline -Comparison $comparison -VisualBaselinePath $visualBaselinePath -ComparisonManifest $comparisonManifest
    }

    if (-not $SkipPack) {
        $packageRoot = Join-Path $root "ChartForgeX/bin/$Configuration"
        if (Test-Path $packageRoot) {
            Get-ChildItem $packageRoot -Filter 'ChartForgeX*.nupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
            Get-ChildItem $packageRoot -Filter 'ChartForgeX*.snupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
        }

        dotnet pack $library -c $Configuration --no-build
        $packages = @(Get-ChildItem $packageRoot -Filter 'ChartForgeX*.nupkg' | Sort-Object Name)
        if ($packages.Count -ne 1) {
            throw "Expected exactly one package, found $($packages.Count)."
        }
        $package = $packages[0]
        if (-not $package) {
            throw 'Package was not created.'
        }
        $symbolsPackages = @(Get-ChildItem $packageRoot -Filter 'ChartForgeX*.snupkg' | Sort-Object Name)
        if ($symbolsPackages.Count -ne 1) {
            throw "Expected exactly one symbol package, found $($symbolsPackages.Count)."
        }
        $symbolsPackage = $symbolsPackages[0]
        if (-not $symbolsPackage) {
            throw 'Symbol package was not created.'
        }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -eq 'ChartForgeX.nuspec' } | Select-Object -First 1
            if (-not $nuspecEntry) {
                throw 'Package is missing ChartForgeX.nuspec.'
            }
            foreach ($requiredEntry in @('README.md', 'CHANGELOG.md')) {
                if (-not ($archive.Entries | Where-Object { $_.FullName -eq $requiredEntry } | Select-Object -First 1)) {
                    throw "Package is missing $requiredEntry."
                }
            }
            foreach ($framework in @('net472', 'netstandard2.0', 'net8.0', 'net10.0')) {
                foreach ($extension in @('dll', 'xml')) {
                    $requiredEntry = "lib/$framework/ChartForgeX.$extension"
                    if (-not ($archive.Entries | Where-Object { $_.FullName -eq $requiredEntry } | Select-Object -First 1)) {
                        throw "Package is missing $requiredEntry."
                    }
                }
            }

            $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
            try {
                $nuspec = $reader.ReadToEnd()
            } finally {
                $reader.Dispose()
            }

            if ($nuspec -match '<dependency\s') {
                throw 'Core package must not contain runtime NuGet dependencies.'
            }
        } finally {
            $archive.Dispose()
        }

        $packageVersion = [System.IO.Path]::GetFileNameWithoutExtension($package.Name).Substring('ChartForgeX.'.Length)
        $consumerRoot = Join-Path ([System.IO.Path]::GetTempPath()) "ChartForgeX-package-consumer-$([Guid]::NewGuid().ToString('N'))"
        try {
            New-Item -ItemType Directory -Path $consumerRoot | Out-Null
            Push-Location $consumerRoot
            try {
                dotnet new console --framework net8.0 --no-restore | Out-Null
                @"
<configuration>
  <packageSources>
    <clear />
    <add key="local-chartforgex" value="$packageRoot" />
  </packageSources>
</configuration>
"@ | Set-Content -Path (Join-Path $consumerRoot 'NuGet.config') -Encoding UTF8
                dotnet add package ChartForgeX --version $packageVersion --source $packageRoot | Out-Null
                @"
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

var chart = Chart.Create()
    .WithTitle("Package smoke")
    .WithSize(320, 180)
    .AddLine("Values", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });

if (!chart.ToSvg().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("SVG render failed.");
if (!chart.ToHtmlFragment().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("HTML render failed.");
if (chart.ToPng().Length <= 64) throw new InvalidOperationException("PNG render failed.");
"@ | Set-Content -Path (Join-Path $consumerRoot 'Program.cs') -Encoding UTF8
                dotnet run -c Release --no-restore | Out-Null
            } finally {
                Pop-Location
            }
        } finally {
            if (Test-Path $consumerRoot) {
                Remove-Item $consumerRoot -Recurse -Force
            }
        }
    }
} finally {
    Pop-Location
}

# Releasing

This repository is private, so releases should run on the private self-hosted GitHub Actions runner.

## Checklist

1. Choose and add package license metadata before a public NuGet release.
2. Update `Version` and `PackageReleaseNotes` in `ChartForgeX/ChartForgeX.csproj`.
3. Move relevant entries from `CHANGELOG.md` `Unreleased` into the release version.
4. Run:

```powershell
./Build.ps1 -Configuration Release
```

5. Inspect generated examples at `ChartForgeX.Examples/bin/Release/net8.0/output/index.html`.
6. Publish both package files from `ChartForgeX/bin/Release`:

```text
ChartForgeX.<version>.nupkg
ChartForgeX.<version>.snupkg
```

## Package Invariants

- The core `.nupkg` must not contain runtime NuGet dependencies.
- `README.md` and `CHANGELOG.md` must be included in the package root.
- XML documentation should be present for every target framework.
- The `.snupkg` symbol package must be generated alongside the `.nupkg`.
- A clean temporary `net8.0` console app must be able to install the freshly packed local package and render SVG, HTML, and PNG output.

## Visual Baseline

Release builds validate `ChartForgeX.Examples/visual-baseline.json` against the generated SVG/PNG comparison manifest. The baseline tracks SVG dimensions, high-DPI PNG output scale, minimum SVG visual-node counts, minimum PNG visible-pixel/color counts, clipped SVG text, near-edge SVG text, and PNG edge pressure.

Before refreshing the baseline, inspect:

- `ChartForgeX.Examples/bin/Release/net8.0/output/quality-dashboard.html`
- `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.html`
- `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.json`

If a visual change is intentional, refresh the baseline with:

```powershell
./Build.ps1 -Configuration Release -UpdateVisualBaseline
```

Commit the refreshed baseline only with the renderer or example change that justifies it. Do not refresh the baseline to hide warnings, clipped text, edge pressure, or accidental PNG/SVG parity regressions.

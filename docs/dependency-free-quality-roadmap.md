# Dependency-Free Quality Roadmap

ChartForgeX should stay a dependency-free rendering engine. The goal is not to wrap Skia, ImageSharp, browser canvas, or another drawing stack. The goal is to produce polished SVG, HTML, PNG, and later formats from our own chart model and rendering code.

## Product Position

- Keep `ChartForgeX` free of runtime package dependencies.
- Treat SVG as the highest-fidelity static output.
- Make PNG progressively better through the built-in rasterizer.
- Prefer deterministic output suitable for reports, dashboards, email, documentation, and package tests.
- Add new formats only when they can share the same chart model, layout rules, and quality standards.

## Rendering Direction

The long-term rendering model should move toward a shared scene/layout pipeline:

1. Validate chart data and options.
2. Compute chart ranges, ticks, label reserves, and collision-aware geometry.
3. Produce renderer-independent drawing instructions.
4. Emit those instructions through SVG, PNG, HTML, and future backends.

This keeps output consistent without outsourcing the hard parts to an external graphics dependency.

## SVG Quality Work

- Continue using semantic role markers for tests and accessibility.
- Improve label collision handling for data labels, legends, and dense axes.
- Add richer but deterministic styling tokens for report presets.
- Improve combo chart composition and multiple axis support.
- Keep generated SVG self-contained, script-free, and safe for static HTML.

## PNG Quality Work

- Improve alpha-correct compositing and downsampling.
- Expose bounded PNG quality controls without changing requested output dimensions.
- Expand antialiasing coverage for paths, arcs, joins, caps, and polygons.
- Improve cubic Bezier flattening and stroke joins.
- Add more gradient support to match SVG styling where practical.
- Continue improving the dependency-free TrueType path for measurement, kerning, and glyph rendering.
- Add explicit visual regression coverage for PNG output, not only byte or dimension checks.

## Chart Coverage

New chart types should ship only when SVG and PNG both meet the library's visual bar.

Strong candidates:

- Streamgraph-style stacked area
- Multi-series reference bands
- Small-multiple composition helpers

## Format Coverage

Future formats should be built as native emitters over the shared scene model:

- SVG: vector backend
- HTML: static inline SVG wrapper
- PNG: dependency-free raster backend
- PDF: future dependency-free vector writer
- Office-friendly formats: evaluated later based on real report needs

Runtime graphics dependencies are not part of the core roadmap.

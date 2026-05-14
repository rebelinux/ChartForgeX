using System;
using System.Collections.Generic;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterDocument {
    public SvgRasterDocument(SvgRasterViewBox viewBox, IReadOnlyList<SvgRasterElement> children) {
        ViewBox = viewBox;
        Children = children ?? throw new ArgumentNullException(nameof(children));
    }

    public SvgRasterViewBox ViewBox { get; }

    public IReadOnlyList<SvgRasterElement> Children { get; }
}

internal sealed class SvgRasterElement {
    private readonly Dictionary<string, string> _attributes;

    public SvgRasterElement(string name, Dictionary<string, string> attributes, IReadOnlyList<SvgRasterElement> children, string text) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        Children = children ?? throw new ArgumentNullException(nameof(children));
        Text = text ?? string.Empty;
    }

    public string Name { get; }

    public IReadOnlyList<SvgRasterElement> Children { get; }

    public string Text { get; }

    public string? Get(string name) =>
        _attributes.TryGetValue(name, out var value) ? value : null;

    public bool TryGet(string name, out string value) =>
        _attributes.TryGetValue(name, out value!);
}

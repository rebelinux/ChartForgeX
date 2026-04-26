using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var output = Path.Combine(AppContext.BaseDirectory, "output");
Directory.CreateDirectory(output);

var dnssec = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML and PNG chart rendering")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(ChartTheme.Dark())
    .WithSize(1180, 640)
    .WithTransparentBackground(true)
    .AddArea("Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230, 1260))
    .AddLine("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36))
    .AddLine("Failed", Points(22, 30, 28, 21, 18, 15, 13, 10), ChartColor.FromRgb(248, 113, 113));

dnssec.SaveSvg(Path.Combine(output, "domain-security-dark.svg"));
dnssec.SaveHtml(Path.Combine(output, "domain-security-dark.html"));
dnssec.SavePng(Path.Combine(output, "domain-security-dark.png"));

var bars = Chart.Create()
    .WithTitle("Certificate Transparency Volume")
    .WithSubtitle("Static report chart with no JavaScript runtime")
    .WithXAxis("Day")
    .WithYAxis("Certificates")
    .WithTheme(ChartTheme.Light())
    .WithSize(1180, 640)
    .AddBar("Certificates", Points(4200, 5300, 6100, 5900, 7200, 8100, 7900));

bars.SaveSvg(Path.Combine(output, "ct-volume-light.svg"));
bars.SaveHtml(Path.Combine(output, "ct-volume-light.html"));
bars.SavePng(Path.Combine(output, "ct-volume-light.png"));

Console.WriteLine("Generated files in: " + output);

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
}

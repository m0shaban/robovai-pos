using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Generates a clean, original icon for "RoboVAI POS".
// Output:
//   assets/branding/robovai-pos.ico
//   assets/branding/robovai-pos-256.png

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var outDir = Path.Combine(repoRoot, "assets", "branding");
Directory.CreateDirectory(outDir);

var icoPath = Path.Combine(outDir, "robovai-pos.ico");
var pngPath = Path.Combine(outDir, "robovai-pos-256.png");

var sizes = new[] { 16, 32, 48, 64, 128, 256 };
var pngImages = sizes.Select(s => RenderPng(s)).ToList();

File.WriteAllBytes(icoPath, BuildIcoFromPngs(pngImages));
File.WriteAllBytes(pngPath, pngImages.Last());

Console.WriteLine($"Wrote: {icoPath}");
Console.WriteLine($"Wrote: {pngPath}");

static byte[] RenderPng(int size)
{
    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);

    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

    g.Clear(Color.Transparent);

    // Background: dark rounded square
    var pad = Math.Max(1, size / 16);
    var rect = new Rectangle(pad, pad, size - 2 * pad, size - 2 * pad);
    using (var bgPath = RoundedRect(rect, Math.Max(3, size / 6)))
    using (var bgBrush = new SolidBrush(Color.FromArgb(255, 18, 20, 28)))
    {
        g.FillPath(bgBrush, bgPath);
    }

    // Accent gradient stroke
    using (var strokePath = RoundedRect(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), Math.Max(3, size / 6)))
    using (var pen = new Pen(Color.FromArgb(255, 94, 232, 199), Math.Max(1, size / 48f)))
    {
        g.DrawPath(pen, strokePath);
    }

    // Simple "POS terminal" glyph (original)
    var w = rect.Width;
    var h = rect.Height;

    var bodyW = (int)(w * 0.62);
    var bodyH = (int)(h * 0.40);
    var bodyX = rect.X + (w - bodyW) / 2;
    var bodyY = rect.Y + (int)(h * 0.30);

    using var bodyPath = RoundedRect(new Rectangle(bodyX, bodyY, bodyW, bodyH), Math.Max(2, size / 12));
    using var bodyBrush = new SolidBrush(Color.FromArgb(255, 32, 36, 52));
    g.FillPath(bodyBrush, bodyPath);

    // Screen
    var screenPad = (int)(bodyW * 0.08);
    var screen = new Rectangle(bodyX + screenPad, bodyY + screenPad, bodyW - 2 * screenPad, (int)(bodyH * 0.45));
    using (var screenPath = RoundedRect(screen, Math.Max(2, size / 24)))
    using (var screenBrush = new SolidBrush(Color.FromArgb(255, 22, 160, 133)))
    {
        g.FillPath(screenBrush, screenPath);
    }

    // Keys
    var keysY = screen.Bottom + (int)(bodyH * 0.10);
    var keyH = (int)(bodyH * 0.18);
    var keyGap = Math.Max(1, size / 64);
    var keyW = (screen.Width - 2 * keyGap) / 3;

    for (int i = 0; i < 3; i++)
    {
        var key = new Rectangle(screen.Left + i * (keyW + keyGap), keysY, keyW, keyH);
        using var keyPath = RoundedRect(key, Math.Max(2, size / 32));
        using var keyBrush = new SolidBrush(Color.FromArgb(255, 55, 61, 84));
        g.FillPath(keyBrush, keyPath);
    }

    // Receipt
    var receiptW = (int)(bodyW * 0.34);
    var receiptH = (int)(bodyH * 0.34);
    var receiptX = bodyX + (bodyW - receiptW) / 2;
    var receiptY = bodyY - (int)(receiptH * 0.55);
    var receiptRect = new Rectangle(receiptX, receiptY, receiptW, receiptH);

    using (var receiptPath = RoundedRect(receiptRect, Math.Max(2, size / 32)))
    using (var receiptBrush = new SolidBrush(Color.FromArgb(255, 236, 240, 241)))
    {
        g.FillPath(receiptBrush, receiptPath);
    }

    // Lines on receipt
    using (var linePen = new Pen(Color.FromArgb(180, 44, 62, 80), Math.Max(1, size / 128f)))
    {
        var l1 = receiptY + (int)(receiptH * 0.35);
        var l2 = receiptY + (int)(receiptH * 0.60);
        g.DrawLine(linePen, receiptX + (int)(receiptW * 0.15), l1, receiptX + (int)(receiptW * 0.85), l1);
        g.DrawLine(linePen, receiptX + (int)(receiptW * 0.15), l2, receiptX + (int)(receiptW * 0.70), l2);
    }

    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    return ms.ToArray();
}

static GraphicsPath RoundedRect(Rectangle bounds, int radius)
{
    var path = new GraphicsPath();
    int d = radius * 2;

    path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
    path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
    path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
    path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
    path.CloseFigure();

    return path;
}

static byte[] BuildIcoFromPngs(List<byte[]> pngImages)
{
    // ICO with PNG entries. Format:
    // ICONDIR (6 bytes) + ICONDIRENTRY (16 bytes each) + image data.

    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);

    // ICONDIR
    bw.Write((ushort)0); // reserved
    bw.Write((ushort)1); // type=icon
    bw.Write((ushort)pngImages.Count);

    var offset = 6 + (16 * pngImages.Count);

    // Need sizes for directory entries; decode PNG header IHDR.
    var entries = pngImages.Select(p => ReadPngSize(p)).ToList();

    for (int i = 0; i < pngImages.Count; i++)
    {
        var (w, h) = entries[i];
        var img = pngImages[i];

        bw.Write((byte)(w >= 256 ? 0 : w));
        bw.Write((byte)(h >= 256 ? 0 : h));
        bw.Write((byte)0); // colors
        bw.Write((byte)0); // reserved
        bw.Write((ushort)1); // planes
        bw.Write((ushort)32); // bpp
        bw.Write(img.Length);
        bw.Write(offset);

        offset += img.Length;
    }

    foreach (var img in pngImages)
    {
        bw.Write(img);
    }

    bw.Flush();
    return ms.ToArray();
}

static (int Width, int Height) ReadPngSize(byte[] png)
{
    // PNG signature (8) + IHDR length(4) + 'IHDR'(4) + width(4) + height(4)
    // Width/Height are big-endian.
    if (png.Length < 24)
    {
        return (256, 256);
    }

    int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
    int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
    return (w, h);
}

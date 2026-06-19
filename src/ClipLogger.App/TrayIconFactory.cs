using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ClipLogger.App;

/// <summary>Draws a small clipboard glyph at runtime so the app has a distinct tray icon.</summary>
public static class TrayIconFactory
{
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var body = new SolidBrush(Color.FromArgb(45, 95, 170));
            using var bodyPath = Rounded(new Rectangle(6, 6, 20, 23), 4);
            g.FillPath(body, bodyPath);

            using var paper = new SolidBrush(Color.White);
            g.FillRectangle(paper, 9, 11, 14, 13);

            using var clip = new SolidBrush(Color.FromArgb(205, 210, 220));
            using var clipPath = Rounded(new Rectangle(12, 3, 8, 6), 2);
            g.FillPath(clip, clipPath);

            using var line = new Pen(Color.FromArgb(70, 120, 190), 2);
            g.DrawLine(line, 11, 15, 21, 15);
            g.DrawLine(line, 11, 19, 21, 19);
            g.DrawLine(line, 11, 23, 18, 23);
        }

        var hicon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hicon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hicon);
        }
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

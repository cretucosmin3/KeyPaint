using System.Numerics;
using SkiaSharp;
using static KeyPaint.DrawEngine;

namespace KeyPaint;

public static class AreaHelper
{
    public static SKRect ShiftFocusArea(SKRect rect, SKRect bounds, float shiftX, float shiftY)
    {
        float Width = rect.Right - rect.Left;
        float Height = rect.Bottom - rect.Top;

        SKRect result = new(
            rect.Left + (Width * shiftX),
            rect.Top + (Height * shiftY),
            rect.Right + (Width * shiftX),
            rect.Bottom + (Height * shiftY)
        );

        if (bounds.Contains(result)) return result;

        return rect;
    }

    public static SKRect ShiftFocusAreaByHalf(SKRect rect, SKRect bounds, float shiftX, float shiftY)
    {
        float Width = (rect.Right - rect.Left) / 2f;
        float Height = (rect.Bottom - rect.Top) / 2f;

        SKRect result = new(
            rect.Left + (Width * shiftX),
            rect.Top + (Height * shiftY),
            rect.Right + (Width * shiftX),
            rect.Bottom + (Height * shiftY)
        );

        if (bounds.Contains(result)) return result;

        return rect;
    }
}
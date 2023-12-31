using System.Numerics;
using SkiaSharp;
using static KeyPaint.DrawEngine;

namespace KeyPaint;

public static class AreaHelper
{
    public static SKRect ShiftArea(SKRect rect, SKRect bounds, float shiftX, float shiftY)
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

    public static SKRect ShiftAreaToOpposite(SKRect rect, SKRect bounds, bool horizontalShift)
    {
        float Width = rect.Right - rect.Left;
        float Height = rect.Bottom - rect.Top;

        if (horizontalShift)
        {
            return new(
                bounds.Right - rect.Left - Width,
                rect.Top,
                bounds.Right - rect.Left,
                rect.Bottom
            );
        }

        return new(
            rect.Left,
            bounds.Bottom - rect.Top - Height,
            rect.Right,
            bounds.Bottom - rect.Top
        );
    }

    public static SKRect TrimAreaToDirection(SKRect focusArea, Direction direction)
    {
        SKRect result = new();

        switch (direction)
        {
            case Direction.Left:
                result.Left = focusArea.Left;
                result.Top = focusArea.Top;
                result.Right = focusArea.Left + (focusArea.Width / 2f);
                result.Bottom = focusArea.Bottom;
                break;
            case Direction.Right:
                result.Left = focusArea.Left + (focusArea.Width / 2f);
                result.Top = focusArea.Top;
                result.Right = focusArea.Right;
                result.Bottom = focusArea.Bottom;
                break;
            case Direction.Up:
                result.Left = focusArea.Left;
                result.Top = focusArea.Top;
                result.Right = focusArea.Right;
                result.Bottom = focusArea.Top + (focusArea.Height / 2f);
                break;
            case Direction.Down:
                result.Left = focusArea.Left;
                result.Top = focusArea.Top + (focusArea.Height / 2f);
                result.Right = focusArea.Right;
                result.Bottom = focusArea.Bottom;
                break;
        }

        return result;
    }
}
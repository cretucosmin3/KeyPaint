using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NativeFileDialogSharp;
using SkiaSharp;

namespace KeyPaint;

static class Program
{
    private static GraphicsWindow Window = default!;

    private static readonly SKPaint DrawPaint = new()
    {
        Color = SKColors.Black,
        StrokeWidth = 2,
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
    };

    private static readonly SKPaint FocusAreaPaint = new()
    {
        Color = SKColors.Red.WithAlpha(175),
        StrokeWidth = 3,
        Style = SKPaintStyle.Stroke,
    };

    private static readonly SKPaint FocusPointPaint = new()
    {
        Color = SKColors.IndianRed,
        StrokeWidth = 6,
        Style = SKPaintStyle.Stroke,
    };

    private static readonly SKPaint SelectedFocusAreaPaint = new()
    {
        Color = SKColors.Red.WithAlpha(25),
        Style = SKPaintStyle.Fill,
    };

    static readonly float WindowWidth = 900;
    static readonly float WindowHeight = 750;

    static SKRect FocusArea = new(0, 0, WindowWidth, WindowHeight);
    static SKRect SelectedFocusArea = new(0, 0, WindowWidth, WindowHeight);
    static SKPoint SelectedFocusPoint = new(0, 0);

    static readonly List<SKPoint> DrawPathPoints = new();
    static readonly SKPath CurrentPath = new();

    static SKPathEffect FuzzyEffect = null!;
    static SKPathEffect RoundEffect = null!;
    static float Roundness = 0;
    static float Fuzzyness = 0;

    static bool IsPlacingPoint = false;
    static bool HasAreaFocused = false;
    static bool SelectingRoundnessAndFuzzyness = false;
    static bool IsSelectingThinkness = false;
    static Silk.NET.Input.Key FocusedDirection;

    static readonly SKBitmap DrawingBitmap = new((int)WindowWidth, (int)WindowHeight, true);
    static SKCanvas BitmapCanvas = null!;

    static void Main()
    {
        Window = new GraphicsWindow("KeyPaint", (int)WindowWidth, (int)WindowHeight);
        Window.OnFrame += OnFrame;
        Window.OnLoaded += OnWindowLoaded;
        Window.RenderWaitTicks = 5;

        Window.Start();
    }

    static void OnWindowLoaded()
    {
        // Create canvas for drawing on the drawing bitmap
        BitmapCanvas = new(DrawingBitmap);
        BitmapCanvas.Clear(SKColors.White);

        foreach (var keyboard in Window.Input.Keyboards)
        {
            keyboard.KeyDown += (kb, key, i) =>
            {
                switch (key)
                {
                    case Silk.NET.Input.Key.Number1:
                        DrawPaint.Color = new(235, 235, 235);
                        break;
                    case Silk.NET.Input.Key.Number2:
                        DrawPaint.Color = new(175, 175, 175);
                        break;
                    case Silk.NET.Input.Key.Number3:
                        DrawPaint.Color = new(110, 110, 110);
                        break;
                    case Silk.NET.Input.Key.Number4:
                        DrawPaint.Color = new(50, 50, 50);
                        break;
                    case Silk.NET.Input.Key.Number5:
                        DrawPaint.Color = new(15, 15, 15);
                        break;
                    case Silk.NET.Input.Key.Space:
                        if (IsPlacingPoint) return;

                        ConfirmPathOnBitmap();
                        break;
                    case Silk.NET.Input.Key.Q:
                        BitmapCanvas.Clear(SKColors.White);
                        break;
                    case Silk.NET.Input.Key.S:
                        if (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft))
                        {
                            SaveImageToFile();
                            break;
                        }

                        IsSelectingThinkness = true;
                        break;
                    case Silk.NET.Input.Key.D:
                        SelectingRoundnessAndFuzzyness = true;
                        break;
                    case Silk.NET.Input.Key.F:
                        DrawPaint.Style = DrawPaint.Style == SKPaintStyle.Stroke ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
                        break;
                    case Silk.NET.Input.Key.C:
                        IsPlacingPoint = true;
                        break;
                    case Silk.NET.Input.Key.Left:
                        if (SelectingRoundnessAndFuzzyness)
                        {
                            CalculateRoundness(false);
                            break;
                        }

                        if (!IsPlacingPoint) break;

                        HasAreaFocused = true;
                        FocusedDirection = key;
                        CalculateFocusedArea();
                        RedoCurrentPath();
                        break;
                    case Silk.NET.Input.Key.Right:
                        if (SelectingRoundnessAndFuzzyness)
                        {
                            CalculateRoundness(true);
                            break;
                        }

                        if (!IsPlacingPoint) break;

                        HasAreaFocused = true;
                        FocusedDirection = key;
                        CalculateFocusedArea();
                        RedoCurrentPath();
                        break;
                    case Silk.NET.Input.Key.Up:
                        if (SelectingRoundnessAndFuzzyness)
                        {
                            CalculateFuzzyness(true);
                            break;
                        }

                        if (!IsPlacingPoint) break;

                        HasAreaFocused = true;
                        FocusedDirection = key;
                        CalculateFocusedArea();
                        RedoCurrentPath();
                        break;
                    case Silk.NET.Input.Key.Down:
                        if (SelectingRoundnessAndFuzzyness)
                        {
                            CalculateFuzzyness(false);
                            break;
                        }

                        if (!IsPlacingPoint) break;

                        HasAreaFocused = true;
                        FocusedDirection = key;
                        CalculateFocusedArea();
                        RedoCurrentPath();
                        break;
                }
            };

            keyboard.KeyUp += (kb, key, i) =>
            {
                switch (key)
                {
                    case Silk.NET.Input.Key.S:
                        IsSelectingThinkness = false;
                        break;
                    case Silk.NET.Input.Key.D:
                        SelectingRoundnessAndFuzzyness = false;
                        break;
                    case Silk.NET.Input.Key.C:
                        IsPlacingPoint = false;
                        HasAreaFocused = false;

                        PlaceDrawPoint();
                        break;
                    case Silk.NET.Input.Key.X:
                        DrawPathPoints.Clear();
                        IsPlacingPoint = false;
                        HasAreaFocused = false;

                        RedoCurrentPath();
                        break;
                    case Silk.NET.Input.Key.Z:
                        if (DrawPathPoints.Count > 0)
                        {
                            DrawPathPoints.RemoveAt(DrawPathPoints.Count - 1);
                            IsPlacingPoint = false;
                            HasAreaFocused = false;
                        }
                        if (DrawPathPoints.Count > 0)
                        {
                            SelectedFocusPoint.X = DrawPathPoints[^1].X;
                            SelectedFocusPoint.Y = DrawPathPoints[^1].Y;
                        }

                        RedoCurrentPath();
                        break;
                    case Silk.NET.Input.Key.Left:
                        if (IsSelectingThinkness)
                        {
                            DrawPaint.StrokeWidth = Math.Clamp(DrawPaint.StrokeWidth - 1, 0.5f, 20);
                            break;
                        }

                        if (!IsPlacingPoint) break;

                        if (FocusedDirection == Silk.NET.Input.Key.Left)
                            CalculateSelection();
                        break;
                    case Silk.NET.Input.Key.Right:
                        if (IsSelectingThinkness)
                        {
                            DrawPaint.StrokeWidth = Math.Clamp(DrawPaint.StrokeWidth + 1, 0.5f, 20);
                            break;
                        }

                        if (!IsPlacingPoint) break;

                        if (FocusedDirection == Silk.NET.Input.Key.Right)
                            CalculateSelection();
                        break;
                    case Silk.NET.Input.Key.Up:
                        if (!IsPlacingPoint) break;

                        if (FocusedDirection == Silk.NET.Input.Key.Up)
                            CalculateSelection();
                        break;
                    case Silk.NET.Input.Key.Down:
                        if (!IsPlacingPoint) break;

                        if (FocusedDirection == Silk.NET.Input.Key.Down)
                            CalculateSelection();
                        break;
                }
            };
        }
    }

    static void CalculateRoundness(bool increasing)
    {
        // Dispose of old round effects
        RoundEffect?.Dispose();

        float f = increasing ? 5 : -5;
        Roundness = Math.Clamp(Roundness + f, 0, 200);

        if (Roundness > 0)
            RoundEffect = SKPathEffect.CreateCorner(Roundness);

        ComposePathEffects();
    }

    static void CalculateFuzzyness(bool increasing)
    {
        // Dispose of old fuzzy effects
        FuzzyEffect?.Dispose();

        float f = increasing ? 0.5f : -0.5f;
        Fuzzyness = Math.Clamp(Fuzzyness + f, 0, 10);

        if (Fuzzyness > 0)
            FuzzyEffect = SKPathEffect.CreateDiscrete(Fuzzyness * 1.5f, Fuzzyness);

        ComposePathEffects();
    }

    static void ComposePathEffects()
    {
        bool isRound = Roundness > 0;
        bool isFuzzy = Fuzzyness > 0;

        if (isRound && isFuzzy) // Round and fuzzy
        {
            DrawPaint.PathEffect = SKPathEffect.CreateCompose(FuzzyEffect, RoundEffect);
            return;
        }
        else if (isRound && !isFuzzy) // Round but NOT fuzzy
        {
            FuzzyEffect?.Dispose();
            DrawPaint.PathEffect = RoundEffect;
            return;
        }
        else if (isFuzzy && !isRound) // Fuzzy but NOT round
        {
            RoundEffect?.Dispose();
            DrawPaint.PathEffect = FuzzyEffect;
            return;
        }

        RoundEffect?.Dispose();
        FuzzyEffect?.Dispose();
        DrawPaint.PathEffect?.Dispose();
    }

    static void CalculateFocusedArea()
    {
        float Width = FocusArea.Right - FocusArea.Left;
        float Height = FocusArea.Bottom - FocusArea.Top;

        switch (FocusedDirection)
        {
            case Silk.NET.Input.Key.Left:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Left + (Width / 2f);
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
            case Silk.NET.Input.Key.Right:
                SelectedFocusArea.Left = FocusArea.Left + (Width / 2f);
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
            case Silk.NET.Input.Key.Up:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Top + (Height / 2f);
                break;
            case Silk.NET.Input.Key.Down:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top + (Height / 2f);
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
        }
    }

    static void CalculateSelection()
    {
        FocusArea.Left = SelectedFocusArea.Left;
        FocusArea.Right = SelectedFocusArea.Right;
        FocusArea.Top = SelectedFocusArea.Top;
        FocusArea.Bottom = SelectedFocusArea.Bottom;

        RedoCurrentPath();
    }

    static void PlaceDrawPoint()
    {
        DrawPathPoints.Add(new(
            FocusArea.MidX,
            FocusArea.MidY
        ));

        RedoCurrentPath();

        // Reset area
        FocusArea.Left = 0;
        FocusArea.Top = 0;
        FocusArea.Right = WindowWidth;
        FocusArea.Bottom = WindowHeight;
    }

    static void RedoCurrentPath()
    {
        CurrentPath.Rewind();

        if (DrawPathPoints.Count > 0)
        {
            // Start point
            CurrentPath.MoveTo(DrawPathPoints[0]);

            // Add lines
            for (int i = 1; i < DrawPathPoints.Count; i++)
            {
                CurrentPath.LineTo(DrawPathPoints[i]);
            }
        }

        if (DrawPathPoints.Count > 0 && IsPlacingPoint && HasAreaFocused)
        {
            SelectedFocusPoint.X = SelectedFocusArea.MidX;
            SelectedFocusPoint.Y = SelectedFocusArea.MidY;

            CurrentPath.LineTo(SelectedFocusPoint);
        }
    }

    static void ConfirmPathOnBitmap()
    {
        if (DrawPathPoints.Count > 1)
        {
            BitmapCanvas.DrawPath(CurrentPath, DrawPaint);
        }

        CurrentPath.Rewind();
        DrawPathPoints.Clear();
    }

    static void SaveImageToFile()
    {
        var dialogResult = Dialog.FileSave("png");

        if (dialogResult.IsOk)
        {
            string filePath = dialogResult.Path;
            if (!filePath.Contains(".png"))
            {
                filePath += ".png";
            }

            using var data = DrawingBitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);

            // save the data to a stream
            data.SaveTo(stream);
        }
    }

    static void OnFrame(SKCanvas canvas)
    {
        canvas.DrawBitmap(DrawingBitmap, 0, 0);

        if (DrawPathPoints.Count == 1)
        {
            canvas.DrawPoint(DrawPathPoints[0], DrawPaint);
        }

        if (DrawPathPoints.Count > 0)
        {
            canvas.DrawPath(CurrentPath, DrawPaint);
        }

        if (IsPlacingPoint)
        {
            canvas.DrawRect(FocusArea, FocusAreaPaint);

            if (HasAreaFocused)
            {
                canvas.DrawRect(SelectedFocusArea, SelectedFocusAreaPaint);

                SelectedFocusPoint.X = SelectedFocusArea.MidX;
                SelectedFocusPoint.Y = SelectedFocusArea.MidY;
                FocusPointPaint.StrokeWidth = Math.Clamp(DrawPaint.StrokeWidth + 3, 6, 100);
                canvas.DrawPoint(SelectedFocusPoint, FocusPointPaint);
            }
        }
    }
}
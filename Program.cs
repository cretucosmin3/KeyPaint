using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NativeFileDialogSharp;
using SkiaSharp;

namespace KeyPaint
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int FreeConsole();

        private static GraphicsWindow Window = default!;

        const float WindowWidth = 1100;
        const float WindowHeight = 800;

        // Drawing
        static SKRect FocusArea = new(0, 0, WindowWidth, WindowHeight);
        static SKRect SelectedFocusArea = new(0, 0, WindowWidth, WindowHeight);
        static SKPoint SelectedFocusPoint = new(0, 0);

        // UI
        static readonly SKRoundRect UIPanelArea = new(new(6, 6, 66, 66), 10);
        static readonly SKPath UILineExample = new();
        static bool DisplayPreview = true;

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
            DetachConsoleWindow();
            LoadUserInterfaceVariables();

            Window = new GraphicsWindow("KeyPaint", (int)WindowWidth, (int)WindowHeight);
            Window.OnFrame += OnFrame;
            Window.OnLoaded += OnWindowLoaded;
            Window.RenderWaitTicks = 5;

            Window.Start();
        }

        static void LoadUserInterfaceVariables()
        {
            UILineExample.MoveTo(18, 52);
            UILineExample.LineTo(52, 20);
            UILineExample.LineTo(52, 52);
            UILineExample.LineTo(18, 20);
        }

        static void OnWindowLoaded()
        {
            // Create canvas for drawing on the drawing bitmap
            BitmapCanvas = new(DrawingBitmap);
            BitmapCanvas.Clear(SKColors.White);

            foreach (var keyboard in Window.Input.Keyboards)
            {
                keyboard.KeyDown += (_, key, _) =>
                {
                    switch (key)
                    {
                        case Silk.NET.Input.Key.Number1:
                            PaintsLibrary.DrawPaint.Color = new(235, 235, 235);
                            break;
                        case Silk.NET.Input.Key.Number2:
                            PaintsLibrary.DrawPaint.Color = new(175, 175, 175);
                            break;
                        case Silk.NET.Input.Key.Number3:
                            PaintsLibrary.DrawPaint.Color = new(110, 110, 110);
                            break;
                        case Silk.NET.Input.Key.Number4:
                            PaintsLibrary.DrawPaint.Color = new(50, 50, 50);
                            break;
                        case Silk.NET.Input.Key.Number5:
                            PaintsLibrary.DrawPaint.Color = new(15, 15, 15);
                            break;
                        case Silk.NET.Input.Key.Tab:
                            DisplayPreview = !DisplayPreview;
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
                            PaintsLibrary.DrawPaint.Style = PaintsLibrary.DrawPaint.Style == SKPaintStyle.Stroke ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
                            break;
                        case Silk.NET.Input.Key.Left:
                            if (SelectingRoundnessAndFuzzyness)
                            {
                                CalculateRoundness(false);
                                break;
                            }

                            if (IsSelectingThinkness)
                            {
                                PaintsLibrary.DrawPaint.StrokeWidth = Math.Clamp(PaintsLibrary.DrawPaint.StrokeWidth - 1, 0.5f, 20);
                                break;
                            }

                            IsPlacingPoint = true;
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

                            if (IsSelectingThinkness)
                            {
                                PaintsLibrary.DrawPaint.StrokeWidth = Math.Clamp(PaintsLibrary.DrawPaint.StrokeWidth + 1, 0.5f, 20);
                                break;
                            }

                            IsPlacingPoint = true;
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

                            IsPlacingPoint = true;
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

                            IsPlacingPoint = true;
                            if (!IsPlacingPoint) break;

                            HasAreaFocused = true;
                            FocusedDirection = key;
                            CalculateFocusedArea();
                            RedoCurrentPath();
                            break;
                    }
                };

                keyboard.KeyUp += (_, key, _) =>
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

                            ResetFocusArea();
                            RedoCurrentPath();
                            break;
                        case Silk.NET.Input.Key.Z:
                            if (IsPlacingPoint)
                            {
                                IsPlacingPoint = false;
                                HasAreaFocused = false;
                                ResetFocusArea();
                                RedoCurrentPath();
                                break;
                            }

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
                            if (!IsPlacingPoint) break;

                            if (FocusedDirection == Silk.NET.Input.Key.Left)
                                CalculateSelection();
                            break;
                        case Silk.NET.Input.Key.Right:
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

        static void DetachConsoleWindow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Running on Windows");
                Console.WriteLine("Detaching the console");
                _ = FreeConsole();
            }
        }

        static void CalculateRoundness(bool increasing)
        {
            // Dispose of old round effects
            RoundEffect?.Dispose();

            float f = increasing ? 5 : -5;
            Roundness = Math.Clamp(Roundness + f, 0, 200);

            RoundEffect = SKPathEffect.CreateCorner(Roundness);

            ComposePathEffects();
        }

        static void CalculateFuzzyness(bool increasing)
        {
            // Dispose of old fuzzy effects
            FuzzyEffect?.Dispose();

            float f = increasing ? 0.5f : -0.5f;
            Fuzzyness = Math.Clamp(Fuzzyness + f, 0, 10);

            FuzzyEffect = SKPathEffect.CreateDiscrete(Fuzzyness * 1.5f, Fuzzyness);

            ComposePathEffects();
        }

        static void ComposePathEffects()
        {
            bool isRound = RoundEffect != null;
            bool isFuzzy = FuzzyEffect != null;

            PaintsLibrary.DrawPaint.PathEffect = null;

            if (isRound && isFuzzy) // Round and fuzzy
            {
                PaintsLibrary.DrawPaint.PathEffect = SKPathEffect.CreateCompose(FuzzyEffect, RoundEffect);
            }
            else if (isRound && !isFuzzy) // Round but NOT fuzzy
            {
                PaintsLibrary.DrawPaint.PathEffect = RoundEffect;
            }
            else if (isFuzzy && !isRound) // Fuzzy but NOT round
            {
                PaintsLibrary.DrawPaint.PathEffect = FuzzyEffect;
            }
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

        static void ResetFocusArea()
        {
            // Reset area
            FocusArea.Left = 0;
            FocusArea.Top = 0;
            FocusArea.Right = WindowWidth;
            FocusArea.Bottom = WindowHeight;
        }

        static void PlaceDrawPoint()
        {
            DrawPathPoints.Add(new(
                FocusArea.MidX,
                FocusArea.MidY
            ));

            RedoCurrentPath();
            ResetFocusArea();
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
                BitmapCanvas.DrawPath(CurrentPath, PaintsLibrary.DrawPaint);
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
                canvas.DrawPoint(DrawPathPoints[0], PaintsLibrary.DrawPaint);
            }

            if (DrawPathPoints.Count > 0)
            {
                canvas.DrawPath(CurrentPath, PaintsLibrary.DrawPaint);
            }

            if (IsPlacingPoint)
            {
                canvas.DrawRect(FocusArea, PaintsLibrary.FocusAreaPaint);

                if (HasAreaFocused)
                {
                    canvas.DrawRect(SelectedFocusArea, PaintsLibrary.SelectedFocusAreaPaint);

                    SelectedFocusPoint.X = SelectedFocusArea.MidX;
                    SelectedFocusPoint.Y = SelectedFocusArea.MidY;
                    PaintsLibrary.FocusPointPaint.StrokeWidth = Math.Clamp(PaintsLibrary.DrawPaint.StrokeWidth + 3, 6, 100);

                    canvas.DrawPoint(SelectedFocusPoint, PaintsLibrary.FocusPointPaint);

                    // Draw horizontal line of the cross
                    canvas.DrawLine(0, SelectedFocusPoint.Y, WindowWidth, SelectedFocusPoint.Y, PaintsLibrary.CrossPointPaint);

                    // Draw vertical line of the cross
                    canvas.DrawLine(SelectedFocusPoint.X, 0, SelectedFocusPoint.X, WindowHeight, PaintsLibrary.CrossPointPaint);
                }
            }

            if (DisplayPreview)
                DrawUserInterface(canvas);
        }

        static void DrawUserInterface(SKCanvas canvas)
        {
            bool DrawingPointTooClose = SelectedFocusPoint.X < 150 && SelectedFocusPoint.Y < 150;
            bool ShowToRight = IsPlacingPoint && DrawingPointTooClose;

            var previousColor = PaintsLibrary.DrawPaint.Color;
            PaintsLibrary.DrawPaint.Color = SKColors.Black;

            if (ShowToRight)
            {
                UIPanelArea.Offset(WindowWidth - 71, 0);
                UILineExample.Offset(WindowWidth - 71, 0);
            }

            canvas.DrawRoundRect(UIPanelArea, PaintsLibrary.UIPanel);
            canvas.DrawRoundRect(UIPanelArea, PaintsLibrary.UIPanelOutline);

            canvas.DrawPath(UILineExample, PaintsLibrary.DrawPaint);

            PaintsLibrary.DrawPaint.Color = previousColor;

            if (ShowToRight)
            {
                UIPanelArea.Offset(0 - (WindowWidth - 71), 0);
                UILineExample.Offset(0 - (WindowWidth - 71), 0);
            }
        }
    }
}
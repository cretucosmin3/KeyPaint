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
        private static readonly DrawEngine DrawEngine = new();

        const int WindowWidth = 900;
        const int WindowHeight = 700;

        static void Main()
        {
            DetachConsoleWindow();

            Window = new GraphicsWindow("KeyPaint", WindowWidth, WindowHeight);
            Window.OnFrame += OnFrame;
            Window.OnLoaded += OnWindowLoaded;

            Window.Start();
        }

        static void OnWindowLoaded()
        {
            foreach (var keyboard in Window.Input.Keyboards)
            {
                keyboard.KeyDown += (_, key, _) => DrawEngine.KeyMapper.HandleKeyDown(key);
                keyboard.KeyUp += (_, key, _) => DrawEngine.KeyMapper.HandleKeyUp(key);
            }

            DrawEngine.SetCanvasSize(WindowWidth, WindowHeight);
            DrawEngine.OnRenderRequest += Window.RequestNewFrame;

            Window.RequestNewFrame();
        }

        static void DetachConsoleWindow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Running on Windows");
                // Console.WriteLine("Detaching the console");
                // _ = FreeConsole();
            }
        }

        static void OnFrame(SKCanvas canvas)
        {
            DrawEngine.DrawOnCanvas(canvas);
        }
    }
}
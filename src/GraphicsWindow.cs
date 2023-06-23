using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace KeyPaint
{
    public class GraphicsWindow
    {
        #region Rendering

        private static SKSurface Surface = default!;
        private static GRBackendRenderTarget RenderTarget = default!;
        private static GRGlInterface grGlInterface = default!;
        private static GRContext grContext = default!;
        private static SKCanvas Canvas { get; set; } = default!;

        #endregion

        private IWindow window = default!;
        private IInputContext _Input = default!;
        private readonly string WindowTitle = "KeyPaint";
        private readonly int windowWidth;
        private readonly int windowHeight;

        public int RenderWaitTicks = 1;
        public IInputContext Input => _Input;
        public Action<SKCanvas> OnFrame = default!;
        public Action OnLoaded = default!;

        public GraphicsWindow(string windowTitle, int width = 900, int height = 700)
        {
            WindowTitle = windowTitle;
            windowWidth = width;
            windowHeight = height;
        }

        public void Start()
        {
            SetWindow();
        }

        private void SetWindow()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(windowWidth, windowHeight);
            options.Title = WindowTitle;
            options.VSync = true;
            options.TransparentFramebuffer = false;
            options.WindowBorder = WindowBorder.Fixed;

            GlfwWindowing.Use();

            window = Window.Create(options);

            window.Load += Load;
            window.Render += Render;

            window.Run();
        }

        private void Load()
        {
            _Input = window.CreateInput();

            window.Center();
            SetCanvas(window);
            OnLoaded?.Invoke();

            LoadLogo();
        }

        private void LoadLogo()
        {
            if (!File.Exists("assets/icon128.png")) return;

            unsafe
            {
                using var image = Image.Load<Rgba32>("assets/icon128.png");
                var memoryGroup = image.GetPixelMemoryGroup();
                Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
                var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);

                foreach (var memory in memoryGroup)
                {
                    memory.Span.CopyTo(block);
                    block = block[memory.Length..];
                }

                var icon = new Silk.NET.Core.RawImage(image.Width, image.Height, array);
                window.SetWindowIcon(ref icon);
            }
        }

        private void Render(double time)
        {
            grContext.ResetContext();
            Canvas.Clear(SKColors.White);

            OnFrame.Invoke(Canvas);

            Canvas.Flush();

            if (RenderWaitTicks > 0)
                Thread.Sleep(RenderWaitTicks);
        }

        private static void RenewCanvas(int width, int height)
        {
            RenderTarget?.Dispose();
            Canvas?.Dispose();
            Surface?.Dispose();

            RenderTarget = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
            Surface = SKSurface.Create(grContext, RenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

            Canvas = Surface.Canvas;
        }

        private static void SetCanvas(IWindow window)
        {
            grGlInterface = GRGlInterface.Create();
            grGlInterface.Validate();

            grContext = GRContext.CreateGl(grGlInterface);

            RenewCanvas(window.Size.X, window.Size.Y);

            window.FramebufferResize += newSize =>
            {
                RenewCanvas(newSize.X, newSize.Y);

                window.DoRender();
            };
        }
    }
}
using System;
using System.Threading;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using SkiaSharp;

namespace Graphics;

public class GraphicsWindow
{
    #region Rendering

    private static SKSurface Surface;
    private static GRBackendRenderTarget RenderTarget;
    private static GRGlInterface grGlInterface;
    private static GRContext grContext;
    private static SKCanvas Canvas { get; set; }

    #endregion

    private IWindow window;
    private IInputContext _Input;
    private readonly string WindowTitle = "Graphics";
    private readonly int windowWidth;
    private readonly int windowHeight;

    public int RenderWaitTicks = 1;
    public IInputContext Input => _Input;
    public Action<SKCanvas> OnFrame;
    public Action OnLoaded;

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

    private void RenewCanvas(int width, int height)
    {
        RenderTarget?.Dispose();
        Canvas?.Dispose();
        Surface?.Dispose();

        RenderTarget = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
        Surface = SKSurface.Create(grContext, RenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

        Canvas = Surface.Canvas;
    }

    private void SetCanvas(IWindow window)
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
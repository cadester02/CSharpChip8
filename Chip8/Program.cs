using System.Diagnostics;
using Chip8.Chip8Core;
using Chip8.Services;
using Silk.NET.SDL;

class Program
{
    unsafe static void Main(string[] args)
    {
        Sdl _sdl = null!;
        Window* _window; // Use a pointer for the Window type
        Renderer* _renderer; // Use a pointer for the Renderer type
        float _scale = 10.0f; // Scale factor for the window size

        // Initialize the argument service and chip8 core
        ArgumentService arguments = new ArgumentService(args);
        Chip8Core chip8 = new Chip8Core(arguments);
        Stopwatch stopwatch = new Stopwatch();

        _sdl = Sdl.GetApi();

        // Initialize SDL
        if (_sdl.Init(Sdl.InitVideo) != 0)
        {
            ErrorService.HandleError(ErrorType.UnexpectedError, "Failed to initialize SDL.");
        }

        // Create window
        _window = _sdl.CreateWindow(
            "Chip8 Emulator",
            100,
            100,
            (int)(Constants.SCREEN_WIDTH * _scale),
            (int)(Constants.SCREEN_HEIGHT * _scale),
            (uint)WindowFlags.Shown);

        // Create renderer
        _renderer = _sdl.CreateRenderer(_window, -1, (uint)RendererFlags.Accelerated);

        VideoService videoService = new VideoService(_sdl, _window, _renderer, _scale);

        while (true)
        {
            stopwatch.Restart();

            // Run the chip8 core then display the screen.
            for (int i = 0; i < Constants.INSTRUCTIONS_PER_FRAME; i++)
            {
                chip8.RunChip8();
            }
            videoService.Render(chip8.display);

            // Wait for next frame
            stopwatch.Stop();
            double elapsed = stopwatch.Elapsed.TotalMilliseconds;
            int sleepTime = (int)(Constants.FRAME_TIME - elapsed);

            if (sleepTime > 0)
            {
                System.Threading.Thread.Sleep(sleepTime);
            }
        }
    }
}
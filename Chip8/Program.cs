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
        int _scale = Constants.DEFAULT_SCALE; // Scale factor for the window size

        // Initialize the argument service and chip8 core
        ArgumentService arguments = new ArgumentService(args);
        Chip8Core chip8 = new Chip8Core(arguments);
        Stopwatch stopwatch = new Stopwatch();

        if (arguments.scale.HasValue)
        {
            _scale = arguments.scale.Value;
        }

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
            Constants.SCREEN_WIDTH * _scale,
            Constants.SCREEN_HEIGHT * _scale,
            (uint)WindowFlags.Shown);

        // Create renderer
        _renderer = _sdl.CreateRenderer(_window, -1, (uint)RendererFlags.Accelerated);

        VideoService videoService = new VideoService(_sdl, _window, _renderer, _scale);
        Event sdlEvent = new Event();

        while (true)
        {
            stopwatch.Restart();

            // Check if closing window
            if (_sdl.PollEvent(&sdlEvent) != 0)
            {
                if (sdlEvent.Type == (uint)EventType.Quit)
                {
                    videoService.Dispose();
                    Environment.Exit(0);
                }
            }

            // Decrement timers
            chip8.DecrementTimers();

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
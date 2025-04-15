using System.Diagnostics;
using Chip8.Chip8Core;

class Program
{
    unsafe static void Main(string[] args)
    {
        int _scale = Constants.DEFAULT_SCALE; // Scale factor for the window size

        // Stopwatch for frame timing
        Stopwatch stopwatch = new();

        // Arguments and Chip8 core initialization
        ArgumentService arguments = new(args);
        Chip8Core chip8 = new(arguments);

        // Set screen scale
        if (arguments.Scale.HasValue)
            _scale = arguments.Scale.Value;

        // SDL initialization for video
        SDLService sdlService = new(_scale);
        sdlService.StartSDL();

        while (true)
        {
            stopwatch.Restart();

            // Decrement timers
            chip8.DecrementTimers();

            // Update the keypad state
            chip8.UpdateKeypad(sdlService.HandleKeys());

            chip8.RunChip8();
            sdlService.RenderScreen(chip8.display);

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
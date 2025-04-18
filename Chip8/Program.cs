using System.Diagnostics;
using Chip8.Chip8Core;

class Program
{
    unsafe static void Main(string[] args)
    {
        // Stopwatch for frame timing
        Stopwatch stopwatch = new();

        // Arguments and Chip8 core initialization
        ArgumentService arguments = new(args);
        Chip8Core chip8 = new(arguments);

        // SDL initialization for video
        SDLService sdlService = new(arguments.Scale.HasValue ? arguments.Scale.Value : Constants.DEFAULT_SCALE);
        sdlService.StartSDL();

        // Audio initialization
        AudioService audioService = new();

        while (true)
        {
            stopwatch.Restart();

            // Update the keypad state and run instruction loop
            chip8.RunChip8(sdlService.HandleKeys());

            // Render frame
            sdlService.RenderScreen(chip8.display);

            // Play audio
            if (chip8.soundTimer > 0 && arguments.UseAudio)
                audioService.PlayAudio();
            else
                audioService.StopAudio();

                // Wait for next frame
                stopwatch.Stop();
            double elapsed = stopwatch.Elapsed.TotalMilliseconds;
            int sleepTime = (int)(Constants.FRAME_TIME - elapsed);

            if (sleepTime > 0)
            {
                Thread.Sleep(sleepTime);
            }
        }
    }
}
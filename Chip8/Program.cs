using System.Diagnostics;
using Chip8.Chip8Core;

class Program
{
    static void Main(string[] args)
    {
        // Initialize the argument service and chip8 core
        ArgumentService arguments = new ArgumentService(args);
        Chip8Core chip8 = new Chip8Core(arguments);

        // Run the main loop
        MainLoop(chip8);
    }

    static void MainLoop(Chip8Core chip8)
    {
        // Start frame counter
        Stopwatch stopwatch = new Stopwatch();

        while (true)
        {
            stopwatch.Restart();

            // Run the chip8 core then display the screen.
            for (int i = 0; i < Constants.INSTRUCTIONS_PER_FRAME; i++)
            {
                chip8.RunChip8();
            }
            chip8.DisplayScreen();

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
﻿namespace Chip8.Services
{
    /// <summary>
    /// Handles the command line arguments.
    /// </summary>
    public class ArgumentService
    {
        private readonly string[] Args = Array.Empty<string>();
        public string FilePath { get; private set; } = string.Empty;
        public bool DebugMode { get; private set; } = false;
        public int? Scale { get; private set; } = null;
        public bool UseAudio { get; private set; } = true;

        /// <summary>
        /// Constructor for the class.
        /// Handles the command line arguments.
        /// </summary>
        /// <param name="args">The arguments passed in from main.</param>
        public ArgumentService(string[] args)
        {
            this.Args = args;

            if (args.Length == 0)
                ErrorService.HandleError(ErrorType.MissingArguments, "No arguments provided, use -h or --help for a list of options.");

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    // Enable debug
                    case ("-d"):
                    case ("--debug"):
                        DebugMode = true;
                        break;

                    // File
                    case ("-f"):
                    case ("--file"):
                        if ((i + 1) >= args.Length)
                            ErrorService.HandleError(ErrorType.MissingArguments, "No file provided after -f or --file.");

                        FilePath = args[i + 1];

                        if (!FilePath.Contains(".ch8"))
                            ErrorService.HandleError(ErrorType.InvalidArgument, "File must be a Chip8 rom file. File is not of type .ch8");

                        i++;
                        break;

                    // Scale
                    case ("-s"):
                    case ("--scale"):
                        if ((i + 1) >= args.Length)
                            ErrorService.HandleError(ErrorType.MissingArguments, "No scale provided after -s or --scale.");

                        if (!int.TryParse(args[i + 1], out int parsedScale))
                            ErrorService.HandleError(ErrorType.InvalidArgument, "Invalid scale provided, must be an integer.");

                        if (parsedScale <= 0)
                            ErrorService.HandleError(ErrorType.InvalidArgument, "Invalid scale provided, must be greater than 0.");

                        Scale = parsedScale;
                        i++;
                        break;

                    // Audio
                    case ("-a"):
                    case ("--audio"):
                        UseAudio = false;
                        break;

                    // Help
                    case ("-h"):
                    case ("--help"):
                        PrintHelp();
                        Environment.Exit(0);
                        break;
                }
            }

            if (String.IsNullOrEmpty(FilePath))
                ErrorService.HandleError(ErrorType.MissingFile, "No file provided, use -f or --file to specify a file.");
        }

        /// <summary>
        /// Prints a list of commands to the screen.
        /// </summary>
        public void PrintHelp()
        {
            Console.WriteLine("Chip8 Emulator");
            Console.WriteLine("Usage: Chip8 [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("\t-d, --debug\tEnable debug mode");
            Console.WriteLine("\t-f, --file\tSpecify the file to load");
            Console.WriteLine("\t-s, --scale\tSpecify the scale of the window (default: 10)");
            Console.WriteLine("\t-a, --audio\tDisables audio");
            Console.WriteLine("\t-h, --help \tShow this help message");
        }
    }
}

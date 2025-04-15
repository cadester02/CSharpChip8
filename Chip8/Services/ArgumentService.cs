namespace Chip8.Services
{
    /// <summary>
    /// Handles the command line arguments.
    /// </summary>
    public class ArgumentService
    {
        private string[] args = Array.Empty<string>();
        private string filePath = string.Empty;
        private readonly bool debugMode = false;
        private readonly int? scale = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentService"/> class.
        /// Handles the command line arguments.
        /// </summary>
        /// <param name="args">The arguments passed in from main.</param>
        public ArgumentService(string[] args)
        {
            this.args = args;

            if (args.Length == 0)
            {
                ErrorService.HandleError(ErrorType.MissingArguments, "No arguments provided, use -h or --help for a list of options.");
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    // Enable debug
                    case ("-d"):
                    case ("--debug"):
                        debugMode = true;
                        break;

                    // File
                    case ("-f"):
                    case ("--file"):
                        if ((i + 1) >= args.Length)
                        {
                            ErrorService.HandleError(ErrorType.MissingArguments, "No file provided after -f or --file.");
                        }

                        filePath = args[i + 1];
                        i++;
                        break;

                    // Scale
                    case ("-s"):
                    case ("--scale"):
                        if ((i + 1) >= args.Length)
                        {
                            ErrorService.HandleError(ErrorType.MissingArguments, "No scale provided after -s or --scale.");
                        }

                        if (!int.TryParse(args[i + 1], out int parsedScale))
                        {
                            ErrorService.HandleError(ErrorType.InvalidArgument, "Invalid scale provided, must be an integer.");
                        }

                        scale = parsedScale;
                        i++;
                        break;

                    // Help
                    case ("-h"):
                    case ("--help"):
                        PrintHelp();
                        Environment.Exit(0);
                        break;
                }
            }

            if (String.IsNullOrEmpty(filePath))
            {
                ErrorService.HandleError(ErrorType.MissingFile, "No file provided, use -f or --file to specify a file.");
            }
        }

        /// <summary>
        /// Returns the args.
        /// </summary>
        /// <returns>The args saved in the service.</returns>
        public string[] GetArgs()
        {
            return args;
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
            Console.WriteLine("\t-h, --help \tShow this help message");
        }
    }
}

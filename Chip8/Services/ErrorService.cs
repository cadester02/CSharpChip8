using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8.Services
{
    public enum ErrorType
    {
        OutOfBounds,
        InvalidOpcode,
        EmptyStack,
        MissingFile,
        MissingArguments,
        UnexpectedError,
        InvalidRegister,
        InvalidArgument
    }

    public static class ErrorService
    {
        public static void HandleError(ErrorType? error, string message)
        {
            Console.WriteLine($"Error {error.ToString()}:\t{message}");
            Environment.Exit(1);
        }

        public static void HandleError(string message)
        {
            Console.WriteLine($"Error {ErrorType.UnexpectedError.ToString()}:\t{message}");
            Environment.Exit(1);
        }
    }
}

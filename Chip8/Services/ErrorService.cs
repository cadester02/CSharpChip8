using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8.Services
{
    /// <summary>
    /// Error codes for the program.
    /// </summary>
    public enum ErrorType
    {
        OutOfBounds,
        InvalidOpcode,
        EmptyStack,
        StackOverflow,
        MissingFile,
        MissingArguments,
        UnexpectedError,
        InvalidRegister,
        InvalidArgument,
    }

    /// <summary>
    /// Handles errors from the program.
    /// </summary>
    public static class ErrorService
    {
        /// <summary>
        /// Handles an error with a message. Exits the program.
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="message">The message explaining the error.</param>
        public static void HandleError(ErrorType? error, string message)
        {
            Console.WriteLine($"Error {error.ToString()}:\t{message}");
            Environment.Exit(1);
        }

        /// <summary>
        /// Handles an unexpected error. Exits the program.
        /// </summary>
        /// <param name="message">The message explaining the error.</param>
        public static void HandleError(string message)
        {
            Console.WriteLine($"Error {ErrorType.UnexpectedError.ToString()}:\t{message}");
            Environment.Exit(1);
        }
    }
}

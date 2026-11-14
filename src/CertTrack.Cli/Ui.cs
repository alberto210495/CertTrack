namespace CertTrack.Cli;

public static class Ui
{
    public static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    public static void Success(string message)
    {
        Write(message, ConsoleColor.Green);
        Console.WriteLine();
    }

    // Errors go to stderr so they stay out of piped output.
    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Error: {message}");
        Console.ResetColor();
    }
}

using System.Text;

namespace PhcKnowledge.Install;

public static class ConsolePasswordReader
{
    public static string ReadConfirmedPassword()
    {
        var first = ReadPassword("Password: ");
        var second = ReadPassword("Confirmar password: ");
        if (!string.Equals(first, second, StringComparison.Ordinal))
            throw new InvalidOperationException("As passwords não coincidem.");
        if (string.IsNullOrEmpty(first))
            throw new InvalidOperationException("A password não pode ser vazia.");
        return first;
    }

    public static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                    buffer.Length--;
                continue;
            }

            if (!char.IsControl(key.KeyChar))
                buffer.Append(key.KeyChar);
        }

        return buffer.ToString();
    }
}

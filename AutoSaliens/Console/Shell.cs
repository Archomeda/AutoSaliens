using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SConsole = System.Console;

namespace AutoSaliens.Console
{
    internal static class Shell
    {
        #region ANSI escape support for Windows

        // See: https://www.jerriepelser.com/blog/using-ansi-color-codes-in-net-console-apps/
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        public static bool SupportAnsiColors { get; private set; } = false;

        #endregion

        private static readonly IReadOnlyDictionary<string, string> AnsiColors = new Dictionary<string, string>
        {
            { "{reset}", "\x1b[0m" },
            { "{err}", "\x1b[31;1m" },
            { "{warn}", "\x1b[33;1m" },
            { "{inf}", "\x1b[38;5;39m" },
            { "{verb}", "\x1b[38;5;242m" },
            { "{action}", "\x1b[38;5;147m" },
            { "{logtime}", "\x1b[38;5;242m" },
            { "{command}", "\x1b[38;5;170m" },
            { "{param}", "\x1b[38;5;98m" },
            { "{value}", "\x1b[38;5;85m" },
            { "{url}", "\x1b[38;5;27m" },
            { "{planet}", "\x1b[36;1m" },
            { "{zone}", "\x1b[38;5;208m" },
            { "{svlow}", "\x1b[38;5;193m" },
            { "{slow}", "\x1b[38;5;228m" },
            { "{smed}", "\x1b[38;5;222m" },
            { "{shigh}", "\x1b[38;5;215m"},
            { "{svhigh}", "\x1b[38;5;203m"},
            { "{level}", "\x1b[35;1m" },
            { "{oldlevel}", "\x1b[35m" },
            { "{xp}", "\x1b[32;1m" },
            { "{reqxp}", "\x1b[38;5;190m" },
            { "{oldxp}", "\x1b[32m" }
        };

        private static bool stopRequested = false;
        private static CancellationTokenSource cancellationTokenSource;

        private static StringBuilder inputBuffer = new StringBuilder();
        private static bool readingInput = false;
        private static object consoleLock = new object();

        static Shell()
        {
            var commandType = typeof(ICommand);
            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => commandType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
            Commands = new List<ICommand>(commandTypes.Select(c => (ICommand)Activator.CreateInstance(c))).AsReadOnly();

            SConsole.TreatControlCAsInput = true;

#if NETCOREAPP2_0
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#endif
                try
                {
                    // Only Windows 10 1511 or higher support this
                    // If it doesn't work, oh well... fall back to no colors
                    var stdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                    GetConsoleMode(stdOut, out uint consoleMode);
                    consoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                    if (SetConsoleMode(stdOut, consoleMode))
                        SupportAnsiColors = true;
                }
                catch (Exception) { }
#if NETCOREAPP2_0
            }
            else
                SupportAnsiColors = true;
#endif
        }

        public static IReadOnlyCollection<ICommand> Commands { get; private set; }


        public static Task StartRead()
        {
            cancellationTokenSource = new CancellationTokenSource();
            return Task.Run(async () =>
            {
                while (!stopRequested)
                {
                    // Implement our own reader since Console.ReadLine sucks in async and breaks our CTRL+C interrupt
                    SConsole.Write(">");
                    readingInput = true;
                    int pos = 0;
                    while (true)
                    {
                        var key = SConsole.ReadKey(true);
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && (key.Key == ConsoleKey.C || key.Key == ConsoleKey.Pause))
                        {
                            WriteLine("{verb}Stopping...");
                            await Program.Stop();
                            return;
                        }
                        else if (key.Key == ConsoleKey.Backspace)
                        {
                            if (pos > 0)
                            {
                                inputBuffer.Remove(pos - 1, 1);
                                lock (consoleLock)
                                    SConsole.Write("\b \b");
                                pos--;
                            }
                        }
                        else if (key.Key == ConsoleKey.Delete)
                        {
                            if (pos < inputBuffer.Length)
                            {
                                inputBuffer.Remove(pos, 1);
                                lock (consoleLock)
                                    SConsole.Write(inputBuffer.ToString().Substring(pos) + " " + new string('\b', inputBuffer.Length - pos + 1));
                            }
                        }
                        else if (key.Key == ConsoleKey.LeftArrow)
                        {
                            if (pos > 0)
                            {
                                lock (consoleLock)
                                    SConsole.Write("\b");
                                pos--;
                            }
                        }
                        else if (key.Key == ConsoleKey.RightArrow)
                        {
                            if (pos < inputBuffer.Length)
                            {
                                lock (consoleLock)
                                    SConsole.Write(inputBuffer[pos]);
                                pos++;
                            }
                        }
                        else if (key.Key == ConsoleKey.Home)
                        {
                            if (pos > 0)
                            {
                                lock (consoleLock)
                                    SConsole.Write(new string('\b', pos));
                                pos = 0;
                            }
                        }
                        else if (key.Key == ConsoleKey.End)
                        {
                            if (pos < inputBuffer.Length)
                            {
                                lock (consoleLock)
                                    SConsole.Write(inputBuffer.ToString().Substring(pos));
                                pos = inputBuffer.Length;
                            }
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            lock (consoleLock)
                                SConsole.WriteLine();
                            break;
                        }
                        else if (key.KeyChar != 0)
                        {
                            inputBuffer.Insert(pos, key.KeyChar);
                            pos++;
                            lock (consoleLock)
                            {
                                SConsole.Write(key.KeyChar);
                                if (pos < inputBuffer.Length)
                                    SConsole.Write(inputBuffer.ToString().Substring(pos) + new string('\b', inputBuffer.Length - pos));
                            }
                        }
                    }
                    readingInput = false;

                    var line = inputBuffer.ToString().Trim();
                    inputBuffer.Clear();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var verb = line.Split(new[] { ' ' }, 2)[0];
                    var command = Commands.FirstOrDefault(c => c.Verb == verb);

                    string log = null;
                    if (command != null)
                    {
                        try
                        {
                            string result = await command.Run(line.Substring(verb.Length).Trim(), cancellationTokenSource.Token);
                            if (!string.IsNullOrWhiteSpace(result))
                                log = result;
                            else if (result == null)
                                log = "The command has finished";
                        }
                        catch (OperationCanceledException)
                        {
                            // Fine...
                        }
                        catch (Exception ex)
                        {
                            log = FormatExceptionOutput(ex);
                        }
                    }
                    else
                        log = "Unknown command, use {command}help{reset} to get the list of available commands.";

                    if (log != null)
                        WriteLine(FormatCommandOuput(log), false);
                    WriteLine("", false);
                }
            });
        }

        public static void StopRead()
        {
            stopRequested = true;
            cancellationTokenSource.Cancel();
        }


        public static void WriteLine(string format, params object[] args) => WriteLine(format, true, args);

        public static void WriteLine(string format, bool includeTime, params object[] args)
        {
            if (includeTime)
                format = $"{{logtime}}[{DateTime.Now.ToString("dd HH:mm:ss.fff")}]{{reset}} {format}";

            if (SupportAnsiColors)
                format = AnsiColors.Aggregate($"{format}{{reset}}", (result, kvp) => result.Replace(kvp.Key, kvp.Value));
            else
                format = AnsiColors.Aggregate(format, (result, kvp) => result.Replace(kvp.Key, ""));

            lock (consoleLock)
            {
                if (readingInput)
                {
                    var backspaces = new string('\b', inputBuffer.Length + 1);
                    var wipe = new string(' ', inputBuffer.Length + 1);
                    SConsole.Write(backspaces);
                    SConsole.Write(wipe);
                    SConsole.Write(backspaces);
                }
                SConsole.WriteLine(format, args);
                if (readingInput)
                    SConsole.Write($">{inputBuffer}");
            }
        }

        public static void WriteLines(string[] lines, bool includeTime = true)
        {
            foreach (var line in lines)
                WriteLine(line, includeTime);
        }


        public static string FormatCommandOuput(string text) =>
            string.Join(Environment.NewLine, text.Split(new[] { Environment.NewLine }, StringSplitOptions.None));

        public static string FormatExceptionOutput(Exception ex) => $"An error has occured: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
    }
}

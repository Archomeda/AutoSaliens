using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SConsole = System.Console;

namespace AutoSaliens.Console
{
    internal static class Shell
    {
        private static bool stopRequested = false;
        private static CancellationTokenSource cancellationTokenSource;

        static Shell()
        {
            var commandType = typeof(ICommand);
            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => commandType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
            Commands = new List<ICommand>(commandTypes.Select(c => (ICommand)Activator.CreateInstance(c))).AsReadOnly();

            SConsole.TreatControlCAsInput = true;
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
                    var buf = new StringBuilder();
                    int pos = 0;
                    while (true)
                    {
                        var key = SConsole.ReadKey(true);
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && (key.Key == ConsoleKey.C || key.Key == ConsoleKey.Pause))
                        {
                            WriteLine("Stopping...");
                            await Program.Stop();
                            return;
                        }
                        else if (key.Key == ConsoleKey.Backspace)
                        {
                            if (pos > 0)
                            {
                                buf.Remove(pos - 1, 1);
                                SConsole.Write("\b \b");
                                pos--;
                            }
                        }
                        else if (key.Key == ConsoleKey.Delete)
                        {
                            if (pos < buf.Length - 1)
                            {
                                buf.Remove(pos, 1);
                                SConsole.Write(buf.ToString().Substring(pos) + " " + new string('\b', buf.Length - pos + 1));
                            }
                        }
                        else if (key.Key == ConsoleKey.LeftArrow)
                        {
                            if (pos > 0)
                            {
                                SConsole.Write("\b");
                                pos--;
                            }
                        }
                        else if (key.Key == ConsoleKey.RightArrow)
                        {
                            if (pos < buf.Length - 1)
                            {
                                SConsole.Write(buf[pos]);
                                pos++;
                            }
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            SConsole.WriteLine();
                            break;
                        }
                        else if (key.KeyChar != 0)
                        {
                            buf.Insert(pos, key.KeyChar);
                            pos++;
                            SConsole.Write(key.KeyChar);
                            if (pos < buf.Length)
                                SConsole.Write(buf.ToString().Substring(pos) + new string('\b', buf.Length - pos));
                        }
                    }

                    var line = buf.ToString().Trim();
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
                            if (Program.Debug)
                                throw;
                        }
                    }
                    else
                        log = "Unknown command, use \"help\" to get the list of available commands.";

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


        public static void WriteLine(string format, params string[] args) => WriteLine(format, true, args);

        public static void WriteLine(string format, bool includeTime, params string[] args)
        {
            if (includeTime)
                SConsole.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz")}] {format}", args);
            else
                SConsole.WriteLine(format, args);
        }

        public static void WriteLines(string[] lines) => WriteLines(lines, true);

        public static void WriteLines(string[] lines, bool includeTime)
        {
            foreach (var line in lines)
                WriteLine(line, includeTime);
        }


        public static string FormatCommandOuput(string text) =>
            string.Join("\n", text.Split('\n').Select(l => $"> {l}"));

        public static string FormatExceptionOutput(Exception ex) => $"An error has occured: {ex.Message}\n{ex.StackTrace}";
    }
}

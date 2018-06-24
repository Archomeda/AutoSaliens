using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("strategy")]
    internal class StrategyCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current strategy
                var allValues = Enum.GetValues(typeof(AutomationStrategy)) as AutomationStrategy[];
                var values = allValues.Where(v => Program.Saliens.Strategy.HasFlag(v));
                this.WriteConsole($"The strategy is set to: {string.Join(", ", values.Select(v => v.ToString()))}.");

                this.WriteConsole($@"You can change the strategy by appending any combination of the strategies to this command: strategy <strategy>
where <strategy> is replaced with a list of strategies, seperated by either spaces or commas.

Keep in mind that some strategies are incompatible with each other. If this happens, the first in the defined list below will take priority.
Possible strategies are: {string.Join(", ", allValues.Select(v => v.ToString()))}");

                return "";
            }
            else
            {
                // Set the game time
                string[] strategies = parameters.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    var strategyValues = strategies.Select(s => (AutomationStrategy)Enum.Parse(typeof(AutomationStrategy), s));
                    AutomationStrategy strategy = strategyValues.Aggregate((AutomationStrategy)0, (a, b) => a | b);
                    if (strategy == 0)
                        strategy =
                            AutomationStrategy.MostDifficultPlanetsFirst |
                            AutomationStrategy.MostCompletedPlanetsFirst |
                            AutomationStrategy.MostDifficultZonesFirst |
                            AutomationStrategy.MostCompletedZonesFirst |
                            AutomationStrategy.TopDown;
                    Program.Settings.Strategy = strategy;
                    Program.Settings.Save();
                    return "Your game time has been saved.";
                }
                catch (ArgumentException)
                {
                    return "Invalid input.";
                }
            }
        }
    }
}

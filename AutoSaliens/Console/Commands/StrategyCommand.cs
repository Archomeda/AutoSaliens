using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("strategy")]
    internal class StrategyCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current strategy
                var allValues = Enum.GetValues(typeof(BotStrategy)) as BotStrategy[];
                var values = allValues.Where(v => Program.Settings.Strategy.Value.HasFlag(v));
                this.Logger?.LogCommandOutput($"The strategy is set to: {{value}}{string.Join(", ", values.Select(v => v.ToString()))}{{reset}}.");

                this.Logger?.LogCommandOutput($"You can change the strategy by appending any combination of the strategies to this command: {{command}}strategy {{param}}<strategy>{{reset}}");
                this.Logger?.LogCommandOutput($"where {{param}}<strategy>{{reset}} is replaced with a list of strategies, seperated by either spaces or commas.");
                this.Logger?.LogCommandOutput("");
                this.Logger?.LogCommandOutput($"Keep in mind that some strategies are incompatible with each other.");
                this.Logger?.LogCommandOutput($"If this happens, the first in the defined list below will take priority.");
                this.Logger?.LogCommandOutput($"Possible strategies are:");
                this.Logger?.LogCommandOutput($"  {string.Join($"{Environment.NewLine}  ", allValues.Select(v => $"{{value}}{v}{{reset}}"))}");
            }
            else
            {
                // Set the strategy
                string[] strategies = parameters.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    var strategyValues = strategies.Select(s => (BotStrategy)Enum.Parse(typeof(BotStrategy), s));
                    BotStrategy strategy = strategyValues.Aggregate((BotStrategy)0, (a, b) => a | b);
                    if (strategy == 0)
                        strategy =
                            BotStrategy.MostDifficultPlanetsFirst |
                            BotStrategy.MostCompletedPlanetsFirst |
                            BotStrategy.MostDifficultZonesFirst |
                            BotStrategy.MostCompletedZonesFirst |
                            BotStrategy.TopDown;
                    Program.Settings.Strategy.Value = strategy;
                    this.Logger?.LogCommandOutput("Your strategy has been saved.");
                }
                catch (ArgumentException)
                {
                    this.Logger?.LogCommandOutput("{err}Invalid input.");
                }
            }
        }
    }
}

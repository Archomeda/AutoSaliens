using System;

namespace AutoSaliens.Console
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class CommandVerbAttribute : Attribute
    {
        public CommandVerbAttribute(string verb) => this.Verb = verb;

        public string Verb { get; set; }
    }
}

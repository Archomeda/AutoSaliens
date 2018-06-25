using System;
using AutoSaliens.Api.Models;

namespace AutoSaliens
{
    internal class SaliensApiException : Exception
    {
        public SaliensApiException() : this(0, "Unknown error") { }

        public SaliensApiException(EResult eResult, string message) : base($"Error {eResult.ToString()} ({(int)eResult}): {message}") =>
            this.EResult = eResult;

        public static SaliensApiException FromString(string eResult, string message)
        {
            int.TryParse(eResult, out int eResultInt);
            return new SaliensApiException((EResult)eResultInt, message);
        }

        public EResult EResult { get; set; }
    }
}

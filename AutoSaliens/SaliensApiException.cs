using System;
using AutoSaliens.Api.Models;

namespace AutoSaliens
{
    internal class SaliensApiException : Exception
    {
        public SaliensApiException() : this(0) { }

        public SaliensApiException(EResult eResult) : base($"Error {eResult.ToString()} ({(int)eResult})") =>
            this.EResult = eResult;

        public static SaliensApiException FromString(string eResult)
        {
            int.TryParse(eResult, out int eResultInt);
            return new SaliensApiException((EResult)eResultInt);
        }

        public EResult EResult { get; set; }
    }
}

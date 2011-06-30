using System;
using System.Collections.Generic;

namespace CssDiff
{
    internal class Configuration
    {
        public Configuration()
        {
            Verbosity = "normal";
        }

        public string From { get; set; }
        public string To { get; set; }
        public string Verbosity { get; set; }

        public Verbosity GetVerbosity()
        {
            return (Verbosity) Enum.Parse(typeof (Verbosity), Verbosity, true);
        }

        public IEnumerable<string> GetValidationErrors()
        {
            if (string.IsNullOrWhiteSpace(From))
                yield return "'From' is required";
            if (string.IsNullOrWhiteSpace(To))
                yield return "'To' is required";
            Verbosity val;
            if (!Enum.TryParse(Verbosity, true, out val))
                yield return "'Verbosity' should be 'quiet', 'normal', or 'loud'";
            yield break;
        }
    }
}
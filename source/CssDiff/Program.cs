using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BoneSoft.CSS;
using Mono.Options;

namespace CssDiff
{
    internal class Program
    {
        private static bool _help;

        private static readonly Configuration _config = new Configuration();

        private static readonly OptionSet _option_set = new OptionSet()
            .Add("?|help|h", "show help", option => _help = option != null)
            .Add("f=|from=", "required: the first css file", option => _config.From = option)
            .Add("t=|to=", "required: the second css file", option => _config.To = option)
            .Add("v|verbose", "show parsing errors", option => _config.Verbose = option != null);

        private static void Main(string[] args)
        {
            Console.WriteLine("CssDiff {0}", Assembly.GetExecutingAssembly().GetName().Version);
            try
            {
                _option_set.Parse(args);
            }
            catch (OptionException)
            {
                Help("Error, usage is: ", _option_set);
            }

            ErrorIfNecessary();

            IEnumerable<string> fromCssClasses = GetCssClasses(_config.From, _config.Verbose);

            IEnumerable<string> toCssClasses = GetCssClasses(_config.To, _config.Verbose);

            string[] classesRemoved = fromCssClasses.Except(toCssClasses).OrderBy(x => x).ToArray();

            if (!classesRemoved.Any())
            {
                Console.WriteLine("No classes were removed");
            }
            else
            {
                Console.WriteLine("The following classes appear in the From file but not in the To file:");
                foreach (string css in classesRemoved)
                {
                    Console.WriteLine(css);
                }
            }
        }

        private static IEnumerable<string> GetCssClasses(string file, bool verbose)
        {
            var cssParser = new CSSParser();
            
            CSSDocument cssDocument = cssParser.ParseFile(file);
            
            if (verbose)
                if (cssParser.Errors.Any())
                {
                    Console.WriteLine("Error parsing: {0}", file);
                    foreach (var error in cssParser.Errors)
                    {
                        Console.WriteLine(error);
                    }
                }
                else
                {
                    Console.WriteLine("Successfully parsed: {0}", file);
                }

            return cssDocument.RuleSets
                .SelectMany(x => x.Selectors)
                .SelectMany(x => x.SimpleSelectors)
                .SelectMany(ExtractClassFromSimpleSelector)
                .SelectMany(x => x)
                .Distinct();
        }

        private static IEnumerable<IEnumerable<string>> ExtractClassFromSimpleSelector(SimpleSelector selector)
        {
            if (!string.IsNullOrWhiteSpace(selector.Class)) yield return new[]{selector.Class};
            if (selector.Child != null) yield return ExtractClassFromSimpleSelector(selector.Child).SelectMany(x => x);
        }

        private static void ErrorIfNecessary()
        {
            string errormessage = null;

            if (_help)
                errormessage = "CssDiff.exe /f[rom] VALUE /t[o] VALUE";

            else
                errormessage = string.Join(Environment.NewLine, _config.GetValidationErrors().ToArray());

            if (!string.IsNullOrWhiteSpace(errormessage))
                Help(errormessage, _option_set);
        }

        private static void Help(string message, OptionSet optionSet)
        {
            Console.Error.WriteLine(message);
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }
    }

    internal class Configuration
    {
        public string From { get; set; }
        public string To { get; set; }
        public bool Verbose { get; set; }

        public IEnumerable<string> GetValidationErrors()
        {
            if (string.IsNullOrWhiteSpace(From))
                yield return "'From' is required";
            if (string.IsNullOrWhiteSpace(To))
                yield return "'To' is required";
            yield break;
        }
    }
}
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
            .Add("f=|from=", "required: the first css file name", option => _config.From = option)
            .Add("t=|to=", "required: the second css file name", option => _config.To = option)
            .Add("v:|verbose:", "quiet (only output removed class names), loud (output parse errors)", option => _config.Verbosity = option);

        private static void Main(string[] args)
        {
            try
            {
                _option_set.Parse(args);
            }
            catch (OptionException)
            {
                Version();
                Help("Error, usage is: ", _option_set);
            }
            BasedOnVerbosity(Version, Verbosity.Normal, Verbosity.Loud);

            ErrorIfNecessary();

            IEnumerable<string> fromCssClasses = GetCssClasses(_config.From, _config.GetVerbosity());

            IEnumerable<string> toCssClasses = GetCssClasses(_config.To, _config.GetVerbosity());

            string[] classesRemoved = fromCssClasses.Except(toCssClasses).OrderBy(x => x).ToArray();

            if (!classesRemoved.Any())
            {
                BasedOnVerbosity(() => Console.WriteLine("No classes were removed"), Verbosity.Normal, Verbosity.Loud);
            }
            else
            {
                BasedOnVerbosity(() => Console.WriteLine("The following classes appear in the From file but not in the To file:"), Verbosity.Normal, Verbosity.Loud);
                
                foreach (string css in classesRemoved)
                {
                    Console.WriteLine(css);
                }
            }
        }

        private static void Version()
        {
            Console.WriteLine("CssDiff {0}", Assembly.GetExecutingAssembly().GetName().Version);
        }

        private static IEnumerable<string> GetCssClasses(string file, Verbosity verbose)
        {
            var cssParser = new CSSParser();
            
            CSSDocument cssDocument = cssParser.ParseFile(file);

            BasedOnVerbosity(() =>
                                 {
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
                                 }, Verbosity.Loud);

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
                errormessage = "CssDiff.exe -f=FILE1 -t=FILE2 [-v=[quiet|loud]]";

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

        private static void BasedOnVerbosity(Action action, params Verbosity[] allowed)
        {
            if (allowed.Contains(_config.GetVerbosity()))
                action();
        }
    }


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

    enum Verbosity
    {
        Quiet,
        Normal,
        Loud
    }
}
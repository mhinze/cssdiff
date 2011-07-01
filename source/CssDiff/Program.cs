using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using BoneSoft.CSS;
using Mono.Options;
using RestSharp;

namespace CssDiff
{
    internal class Program
    {
        private static bool _help;

        private static readonly Configuration _config = new Configuration();

        private static readonly OptionSet _option_set = new OptionSet()
            .Add("?|help|h", "show help", option => _help = option != null)
            .Add("f=|from=", "required: the first css", option => _config.From = option)
            .Add("t=|to=", "required: the second css", option => _config.To = option)
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

            IEnumerable<string> fromCssClasses = GetCssClasses(_config.From);

            IEnumerable<string> toCssClasses = GetCssClasses(_config.To);

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

        private static IEnumerable<string> GetCssClasses(string location)
        {
            var cssParser = new CSSParser();

            CSSDocument cssDocument = null;

            if (location.StartsWith("http", true, CultureInfo.InvariantCulture))
            {
                BasedOnVerbosity(
                    () => Console.WriteLine("{0} requested...\n", location), Verbosity.Loud);

                var webClient = new WebClient();
                webClient.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

                string css = webClient.DownloadString(location);

                cssDocument = cssParser.ParseText(css);
            }
            else
            {
                cssDocument = cssParser.ParseFile(location);
            }

            BasedOnVerbosity(() =>
                                 {
                                     if (cssParser.Errors.Any())
                                     {
                                         Console.WriteLine("Error parsing: {0}", location);
                                         foreach (var error in cssParser.Errors)
                                         {
                                             Console.WriteLine(error);
                                         }
                                     }
                                     else
                                     {
                                         Console.WriteLine("Successfully parsed: {0}", location);
                                     }
                                 }, Verbosity.Loud);

            return cssDocument.RuleSets
                .Where(x => x.Selectors != null)
                .SelectMany(x => x.Selectors)
                .Where(x => x.SimpleSelectors != null)
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
                errormessage = "CssDiff.exe -f=LOCATION1 -t=LOCATION2 [-v=[quiet|loud]]";

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

    internal class HttpTextGetter
    {
        readonly Verbosity _verbosity;

        private static void BasedOnVerbosity(Action action, Verbosity current, params Verbosity[] allowed)
        {
            if (allowed.Contains(current))
                action();
        }

        public HttpTextGetter(Verbosity verbosity)
        {
            _verbosity = verbosity;
        }

        public string GetText(string location)
        {
            var restRequest = new RestRequest(location);
            var restResponse = new RestClient().Execute(restRequest);

            BasedOnVerbosity(() => Console.WriteLine("{0} response code: {1}", location, restResponse.StatusDescription), _verbosity, Verbosity.Normal, Verbosity.Loud);

            return restResponse.Content;
        }
    }
}
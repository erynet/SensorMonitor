using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SensorMonitor.Host
{
    class CommandLineOptions
    {
        [Option('l', "library",
            DefaultValue = "NoSuchValue",
            Required = false, HelpText = "specify library(.dll) to load")]
        public string LibName { get; set; }

        [Option('r', "response",
            DefaultValue = 250,
            Required = false, HelpText = "maximum thread response delay in ms")]
        public int ThreadResponseMs { get; set; }

        [Option('i', "install",
            DefaultValue = false,
            MutuallyExclusiveSet = "ServiceInstallCommand",
            Required = false, HelpText = "install client as windows service")]
        public bool InstallService { get; set; }

        [Option('u', "uninstall",
            DefaultValue = false,
            MutuallyExclusiveSet = "ServiceInstallCommand",
            Required = false, HelpText = "uninstall windows service")]
        public bool UninstallService { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

    }
}

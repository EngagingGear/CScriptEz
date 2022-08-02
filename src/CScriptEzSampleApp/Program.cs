using System;
using System.Diagnostics;
using ConsoleTools;

namespace CScriptEzSampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var simpleTestMenu = new ConsoleMenu(args, level: 1)
                .Add("Run Simple Test", () => LaunchScript(@"Scripts\Test.csez"))
                .Add("Return Back", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.ClearConsole = false;
                });

            var testWithMethodsMenu = new ConsoleMenu(args, level: 1)
                .Add("Run Test with Methods", () => LaunchScript(@"Scripts\TestWithMethods.csez"))
                .Add("Return Back", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.ClearConsole = false;
                });

            var testExcelParserMenu = new ConsoleMenu(args, level: 1)
                .Add("Run Excel", () => LaunchScript(@"Scripts\SaveExcelToCsv.csez"))
                .Add("Return Back", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.ClearConsole = false;
                });

            var testNuget = new ConsoleMenu(args, level: 1)
                .Add("Run Nuget Test", () => LaunchScript(@"Scripts\NugetTest.csez"))
                .Add("Return Back", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.ClearConsole = false;
                });

            var menu = new ConsoleMenu(args, level: 0)
                .Add("Simple Test", simpleTestMenu.Show)
                .Add("Test With Methods", testWithMethodsMenu.Show)
                .Add("Save Excel To Csv", testExcelParserMenu.Show)
                .Add("Test Nuget", testNuget.Show)
                .Add("Quit", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.Selector = "-->";
                    config.EnableFilter = false;
                    config.Title = "Select Test Scenario";
                    config.EnableBreadcrumb = true;
                });

            menu.Show();
        }

        private static void LaunchScript(string scriptName)
        {
            var cmdPath = @"CScriptEzTool\CScriptEz.exe";
            
            var startInfo = new ProcessStartInfo(cmdPath);
            startInfo.UseShellExecute = false;
            startInfo.FileName = cmdPath;
            startInfo.ArgumentList.Add(scriptName);
            
            var proc = Process.Start(startInfo);
            proc.WaitForExit();
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="ConsoleMessageSinkActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.MultiNodeTestRunner.Shared.Reporting;
using System.Text;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    /// <summary>
    /// <see cref="MessageSinkActor"/> implementation that logs all of its output directly to the <see cref="Console"/> but formats it using teamcity's custom test runner integration guidelines.
    /// 
    /// Has no persistence capabilities. Can optionally use a <see cref="TestRunCoordinator"/> to provide total "end of test" reporting.
    /// </summary>
    public class TeamCityMessageSinkActor : TestCoordinatorEnabledMessageSink
    {
        public TeamCityMessageSinkActor(bool useTestCoordinator) : base(useTestCoordinator)
        {
        }

        #region Message handling

        protected override void AdditionalReceives()
        {
            Receive<FactData>(data => ReceiveFactData(data));
        }

        protected override void ReceiveFactData(FactData data)
        {
            PrintSpecRunResults(data);
        }

        private void PrintSpecRunResults(FactData data)
        {
            WriteSpecMessage(string.Format("Results for {0}", data.FactName));
            WriteSpecMessage(string.Format("Start time: {0}", new DateTime(data.StartTime, DateTimeKind.Utc)));
            foreach (var node in data.NodeFacts)
            {
                WriteSpecMessage(string.Format(" --> Node {0}: {1} [{2} elapsed]", node.Value.NodeIndex,
                    node.Value.Passed.GetValueOrDefault(false) ? "PASS" : "FAIL", node.Value.Elapsed));
            }
            WriteSpecMessage(string.Format("End time: {0}",
                new DateTime(data.EndTime.GetValueOrDefault(DateTime.UtcNow.Ticks), DateTimeKind.Utc)));
            WriteSpecMessage(string.Format("FINAL RESULT: {0} after {1}.",
                data.Passed.GetValueOrDefault(false) ? "PASS" : "FAIL", data.Elapsed));

            //If we had a failure
            if (data.Passed.GetValueOrDefault(false) == false)
            {
                var details = new StringBuilder();
                details.AppendLine("Failure messages by Node");
                foreach (var node in data.NodeFacts)
                {
                    if (node.Value.Passed.GetValueOrDefault(false) == false)
                    {
                        details.AppendLine(string.Format("<----------- BEGIN NODE {0} ----------->", node.Key));
                        foreach (var resultMessage in node.Value.ResultMessages)
                        {
                            details.AppendLine(String.Format(" --> {0}", resultMessage.Message));
                        }
                        if (node.Value.ResultMessages == null || node.Value.ResultMessages.Count == 0)
                            details.AppendLine("[received no messages - SILENT FAILURE].");
                        details.AppendLine(string.Format("<----------- END NODE {0} ----------->", node.Key));
                    }
                }

                var message = "Spec failed on one of the nodes";
                WriteSpecMessage(string.Format("##teamcity[testFailed name='{0}' message='{1}' details='{2}']", data.FactName,  message, details));
            }
            else
            {
                WriteSpecMessage(string.Format("##teamcity[testFinished name='{0}']", data.FactName));
            }
        }

        protected override void HandleNodeSpecFail(NodeCompletedSpecWithFail nodeFail)
        {
            WriteSpecFail(nodeFail.NodeIndex, nodeFail.Message);

            base.HandleNodeSpecFail(nodeFail);
        }

        protected override void HandleTestRunEnd(EndTestRun endTestRun)
        {
            WriteSpecMessage("Test run complete.");
            
            base.HandleTestRunEnd(endTestRun);
        }

        protected override void HandleTestRunTree(TestRunTree tree)
        {
            var passedSpecs = tree.Specs.Count(x => x.Passed.GetValueOrDefault(false));
            WriteSpecMessage(string.Format("Test run completed in [{0}] with {1}/{2} specs passed.", tree.Elapsed, passedSpecs, tree.Specs.Count()));
            foreach (var factData in tree.Specs)
            {
                PrintSpecRunResults(factData);
            }
        }

        protected override void HandleNewSpec(BeginSpec newSpec)
        {
            WriteSpecMessage(string.Format("##teamcity[testStarted name='{0}.{1}']", newSpec.ClassName, newSpec.MethodName));
            WriteSpecMessage(string.Format("Running on {0} nodes", newSpec.Tests.Count));

            base.HandleNewSpec(newSpec);
        }

        protected override void HandleEndSpec(EndSpec endSpec)
        {
            //WriteSpecMessage(string.Format("##teamcity[testFinished name='{0}.{1}']", endSpec.ClassName, endSpec.MethodName));

            base.HandleEndSpec(endSpec);
        }

        protected override void HandleNodeMessage(LogMessageForNode logMessage)
        {
            WriteNodeMessage(logMessage);

            base.HandleNodeMessage(logMessage);
        }

        protected override void HandleNodeMessageFragment(LogMessageFragmentForNode logMessage)
        {
            WriteNodeMessage(logMessage);

            base.HandleNodeMessageFragment(logMessage);
        }

        protected override void HandleRunnerMessage(LogMessageForTestRunner node)
        {
            WriteRunnerMessage(node);
            
            base.HandleRunnerMessage(node);
        }

        protected override void HandleNodeSpecPass(NodeCompletedSpecWithSuccess nodeSuccess)
        {
            WriteSpecPass(nodeSuccess.NodeIndex, nodeSuccess.Message);

            base.HandleNodeSpecPass(nodeSuccess);
        }

        #endregion

        #region Console output methods

        /// <summary>
        /// Used to print a spec status message (spec starting, finishing, failed, etc...)
        /// </summary>
        private void WriteSpecMessage(string message)
        {
            Console.WriteLine(message);
        }

        private void WriteSpecPass(int nodeIndex, string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[NODE{0}][{1}]: SPEC PASSED: {2}", nodeIndex, DateTime.UtcNow.ToShortTimeString(), message);
            Console.ResetColor();
        }

        private void WriteSpecFail(int nodeIndex, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[NODE{0}][{1}]: SPEC FAILED: {2}", nodeIndex, DateTime.UtcNow.ToShortTimeString(), message);
            Console.ResetColor();
        }

        private void WriteRunnerMessage(LogMessageForTestRunner nodeMessage)
        {
            Console.ForegroundColor = ColorForLogLevel(nodeMessage.Level);
            Console.WriteLine(nodeMessage.ToString());
            Console.ResetColor();
        }

        private void WriteNodeMessage(LogMessageForNode nodeMessage)
        {
            Console.ForegroundColor = ColorForLogLevel(nodeMessage.Level);
            Console.WriteLine(nodeMessage.ToString());
            Console.ResetColor();
        }

        private void WriteNodeMessage(LogMessageFragmentForNode nodeMessage)
        {
            Console.WriteLine(nodeMessage.ToString());
        }

        private static ConsoleColor ColorForLogLevel(LogLevel level)
        {
            var color = ConsoleColor.DarkGray;
            switch (level)
            {
                case LogLevel.DebugLevel:
                    color = ConsoleColor.Gray;
                    break;
                case LogLevel.InfoLevel:
                    color = ConsoleColor.White;
                    break;
                case LogLevel.WarningLevel:
                    color = ConsoleColor.Yellow;
                    break;
                case LogLevel.ErrorLevel:
                    color = ConsoleColor.Red;
                    break;
            }

            return color;
        }

        #endregion
    }

    /// <summary>
    /// <see cref="IMessageSink"/> implementation that formats test runner output to match teamcity's customer test runner integration format.
    /// Uses https://confluence.jetbrains.com/display/TCD4/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingTests
    /// </summary>
    public class TeamCityMessageSink : MessageSink
    {
        public TeamCityMessageSink()
            : base(Props.Create(() => new TeamCityMessageSinkActor(true)))
        {
        }

        protected override void HandleUnknownMessageType(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Unknown message: {0}", message);
            Console.ResetColor();
        }
    }
}


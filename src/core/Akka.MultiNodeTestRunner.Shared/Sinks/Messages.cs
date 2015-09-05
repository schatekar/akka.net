//-----------------------------------------------------------------------
// <copyright file="Messages.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Event;
using System.Linq;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    #region Message types

    /// <summary>
    /// Message type for indicating that a spec has ended.
    /// </summary>
    public class EndSpec
    {
        public EndSpec(IList<NodeTest> tests)
        {
            Tests = tests;

            var firstTest = Tests.First();
            ClassName = firstTest.TestName;
            MethodName = firstTest.MethodName;
        }
        public IList<NodeTest> Tests { get; private set; }
        public string ClassName{ get; private set; }
        public string MethodName { get; private set; }
    }

    /// <summary>
    /// Message type for indicating that a spec has started.
    /// </summary>
    public class BeginSpec
    {
        public BeginSpec(IList<NodeTest> tests)
        {
            Tests = tests;

            var firstTest = Tests.First();
            ClassName = firstTest.TestName;
            MethodName = firstTest.MethodName;
        }
        public IList<NodeTest> Tests { get; private set; }
        public string ClassName { get; private set; }
        public string MethodName { get; private set; }
    }

    /// <summary>
    /// Message type for signaling that a node has completed a spec successfully
    /// </summary>
    public class NodeCompletedSpecWithSuccess
    {
        public NodeCompletedSpecWithSuccess(int nodeIndex, string testName)
        {
            TestName = testName;
            NodeIndex = nodeIndex;
        }

        public string TestName { get; private set; }
        public int NodeIndex { get; private set; }
        public string Message { get; private set; }
    }

    /// <summary>
    /// Message type for signaling that a node has completed a spec unsuccessfully
    /// </summary>
    public class NodeCompletedSpecWithFail
    {
        public NodeCompletedSpecWithFail(int nodeIndex, string testName)
        {
            TestName = testName;
            NodeIndex = nodeIndex;
        }
        public string TestName { get; private set; }
        public int NodeIndex { get; private set; }
        public string Message { get; private set; }
    }

    /// <summary>
    /// Truncated message - cut off from it's parent due to line break in I/O redirection
    /// </summary>
    public class LogMessageFragmentForNode
    {
        public LogMessageFragmentForNode(int nodeIndex, string message, DateTime when)
        {
            NodeIndex = nodeIndex;
            Message = message;
            When = when;
        }

        public int NodeIndex { get; private set; }

        public DateTime When { get; private set; }

        public string Message { get; private set; }

        public override string ToString()
        {
            return string.Format("[NODE{1}][{0}]: {2}", When, NodeIndex, Message);
        }
    }

    /// <summary>
    /// Message for an individual node participating in a spec
    /// </summary>
    public class LogMessageForNode
    {
        public LogMessageForNode(int nodeIndex, string message, LogLevel level, DateTime when, string logSource)
        {
            LogSource = logSource;
            When = when;
            Level = level;
            Message = message;
            NodeIndex = nodeIndex;
        }

        public int NodeIndex { get; private set; }

        public DateTime When { get; private set; }

        public string Message { get; private set; }

        public string LogSource { get; private set; }

        public LogLevel Level { get; private set; }

        public override string ToString()
        {
            return string.Format("[NODE{1}][{0}][{2}][{3}]: {4}", When, NodeIndex,
                Level.ToString().Replace("Level", "").ToUpperInvariant(), LogSource,
                Message);
        }
    }

    /// <summary>
    /// Message for an individual node participating in a spec
    /// </summary>
    public class LogMessageForTestRunner
    {
        public LogMessageForTestRunner(string message, LogLevel level, DateTime when, string logSource)
        {
            LogSource = logSource;
            When = when;
            Level = level;
            Message = message;
        }

        public DateTime When { get; private set; }

        public string Message { get; private set; }

        public string LogSource { get; private set; }

        public LogLevel Level { get; private set; }

        public override string ToString()
        {
            return string.Format("[RUNNER][{0}][{1}][{2}]: {3}", When,
                Level.ToString().Replace("Level", "").ToUpperInvariant(), LogSource,
                Message);
        }
    }


    /// <summary>
    /// Message used to signal the end of the test run.
    /// </summary>
    public class EndTestRun
    {
        
    }

    #endregion
}


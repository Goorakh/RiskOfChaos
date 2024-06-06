using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace RiskOfChaos.Utilities
{
    static class TaskExceptionHandler
    {
        // Assemblies that should be considered "ours",
        // if an exception occurs in one of these, print it using our logger
        static readonly HashSet<Assembly> _ownedAssemblies = [
            Assembly.GetExecutingAssembly(),
            typeof(RiskOfTwitch.Authentication).Assembly
        ];

        public static void Initialize()
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        public static void Cleanup()
        {
            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
        }

        static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (Exception inner in e.Exception.InnerExceptions)
            {
                StackTrace stackTrace = new StackTrace(inner);
                foreach (StackFrame stackFrame in stackTrace.GetFrames())
                {
                    MethodBase method = stackFrame.GetMethod();
                    if (method != null && _ownedAssemblies.Contains(method.DeclaringType.Assembly))
                    {
                        // This is probably ours, print and set observed

                        Log.Error_NoCallerPrefix(e.Exception);
                        e.SetObserved();

                        return;
                    }
                }
            }
        }
    }
}

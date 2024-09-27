using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;

namespace Innovian.Dapr.Workflow.RegistrationAspect;

[CompileTime]
internal static class AspectDiagnostics
{
    public static DiagnosticDefinition<string> CannotHaveDuplicateWorkflowNames { get; } = new("INVN1201",
        Severity.Error, "Duplicate workflow names such as '{0}' will throw an exception at runtime during registration");

    public static DiagnosticDefinition<string> CannotHaveDuplicateWorkflowActivityNames { get; } = new("INVN1202",
        Severity.Error,
        "Duplicate workflow activity names such as '{0}' will throw an exception at runtime during registration");
}
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Innovian.Dapr.Workflow.RegistrationAspect;

public sealed class Fabric : TransitiveProjectFabric
{
    /// <summary>
    /// The user can implement this method to analyze types in the current project, add aspects, and report or suppress diagnostics.
    /// </summary>
    public override void AmendProject(IProjectAmender amender)
    {
        amender.SelectMany(c => c.Types)
            .Where(t => t is {TypeKind: TypeKind.Class, Name: "Program"})
            .AddAspectIfEligible(_ => new WorkflowRegistrationFactoryAttribute());       
    }
}
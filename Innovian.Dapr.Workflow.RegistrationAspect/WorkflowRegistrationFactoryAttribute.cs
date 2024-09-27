using Dapr.Workflow;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Diagnostics;

namespace Innovian.Dapr.Workflow.RegistrationAspect;

[AttributeUsage(AttributeTargets.Class)]
public sealed class WorkflowRegistrationFactoryAttribute : TypeAspect
{
    /// <inheritdoc />
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var workflows = new List<string>();
        var workflowActivities = new List<string>();

        foreach (var type in builder.Target.Compilation.AllTypes)
        {
            if (type is { TypeKind: TypeKind.Class, BaseType.IsAbstract: true })
            {
                switch (type)
                {
                    case { BaseType.Name: "Workflow" }:
                        workflows.Add(type.FullName);
                        break;
                    case { BaseType.Name: "WorkflowActivity" }:
                        workflowActivities.Add(type.FullName);
                        break;
                }
            }
        }

        //Remove any flat-out duplicates by full type name
        workflows = workflows.Distinct().ToList();
        workflowActivities = workflowActivities.Distinct().ToList();

        //Determine if there are any type name duplicates in either the workflows or workflow activities
        IdentifyTypeDuplicates(builder, workflows, AspectDiagnostics.CannotHaveDuplicateWorkflowNames);
        IdentifyTypeDuplicates(builder, workflowActivities, AspectDiagnostics.CannotHaveDuplicateWorkflowActivityNames);
        
        //Introduce a new class
        var builderType = builder
            .With(builder.Target.GetNamespace()!)
            .WithChildNamespace("DaprUtilities")
            .IntroduceClass("DaprRegistrationHelper", OverrideStrategy.Ignore, b =>
            {
                b.IsPartial = true;
                b.IsStatic = true;
                b.Accessibility = Accessibility.Public;
            });

        var workflowRuntimeOptions = (INamedType)TypeFactory.GetType(typeof(WorkflowRuntimeOptions));

        //Register the various workflow and workflow activities in a method introduced into this class
        builderType.IntroduceMethod(nameof(BuildMethodTemplate),
            IntroductionScope.Static,
            buildMethod: b =>
            {
                b.Name = "RegisterAllEntities";
                b.Accessibility = Accessibility.Public;
                b.AddParameter("c", workflowRuntimeOptions);
                //b.AddParameter("serviceCollection", serviceCollectionType); //Need some way to specify 'this' - https://github.com/postsharp/Metalama/issues/339#issuecomment-2251105015
            }, args: new
            {
                workflowTypeNames = workflows,
                workflowActivityTypeNames = workflowActivities
            });

        base.BuildAspect(builder);
    }

    /// <summary>
    /// Perform a duplicate check for type names - duplicates will fail at registration as that's done with their type names, not full names.
    /// </summary>
    /// <param name="builder">The aspect builder interface.</param>
    /// <param name="values">The various values to evaluate.</param>
    /// <param name="diagnosticDefinition">The diagnostic definition suitable to the workflow or workflow activity.</param>
    private static void IdentifyTypeDuplicates(IAspectBuilder builder, List<string> values, DiagnosticDefinition<string> diagnosticDefinition)
    {
        var suffixValues = values
            .Select(value => value.Split('.').Last())
            .GroupBy(s => s)
            .Where(g => g.Count() > 1)
            .Select(a => a.Key)
            .ToList();

        foreach (var suffix in suffixValues)
        {
            builder.Diagnostics.Report(diagnosticDefinition.WithArguments(suffix));
        }
    }

    [Template]
    private static void BuildMethodTemplate([CompileTime] List<string> workflowTypeNames,
        [CompileTime] List<string> workflowActivityTypeNames)
    {
        foreach (var typeName in workflowTypeNames)
        {
            var exprBuilder = new ExpressionBuilder();

            var workflowType = (INamedType)TypeFactory.GetType(typeName);
            exprBuilder.AppendVerbatim("c.RegisterWorkflow<");
            exprBuilder.AppendTypeName(workflowType);
            exprBuilder.AppendVerbatim(">()");

            meta.InsertStatement(exprBuilder.ToExpression());
        }

        foreach (var activityType in workflowActivityTypeNames)
        {
            var exprBuilder = new ExpressionBuilder();

            var workflowType = (INamedType)TypeFactory.GetType(activityType);
            exprBuilder.AppendVerbatim("c.RegisterActivity<");
            exprBuilder.AppendTypeName(workflowType);
            exprBuilder.AppendVerbatim(">()");

            meta.InsertStatement(exprBuilder.ToExpression());
        }
    }
}
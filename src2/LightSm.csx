#!/usr/bin/env dotnet-script
// This is a c# script file

#r "nuget: StateSmith, 0.9.13-alpha-tracking-expander-2" // this line specifies which version of StateSmith to use and download from c# nuget web service.

using StateSmith.Input.Expansions;
using StateSmith.Output.UserConfig;
using StateSmith.Runner;
using StateSmith.SmGraph;  // Note using! This is required to access StateMachine and NamedVertex classes...

public StringBuilder imports = new();
public StringBuilder mocks = new();
public StringBuilder tests = new();


GenerateC99Code();
GenerateSimpleSimulator();

void GenerateC99Code()
{
    SmRunner runner = new(diagramPath: "LightSm.drawio.svg", new LightSmRenderConfig(), transpilerId: TranspilerId.C99);
    runner.Run();
}


void GenerateSimpleSimulator()
{
    SmRunner runner = new(diagramPath: "LightSm.drawio.svg", transpilerId: TranspilerId.JavaScript);
    AddPipelineStep(runner);
    runner.Settings.outputDirectory = "LightSmSim";
    runner.Run();
}




void AddPipelineStep(SmRunner runner)
{
    // This method adds your custom step into the StateSmith transformation pipeline.
    // Some more info here: https://github.com/StateSmith/StateSmith/wiki/How-StateSmith-Works
    runner.SmTransformer.InsertBeforeFirstMatch(StandardSmTransformer.TransformationId.Standard_Validation1, 
                                                new TransformationStep(id: "my custom step blah", ModForSimulation));
}

// This shows roughly how to inspect a state machine.
// This same idea could be used to generate test scaffolding code or to generate documentation.
void ModForSimulation(StateMachine sm)
{
    BehaviorDescriber describer = new(singleLineFormat: true);
    
    sm.VisitRecursively((Vertex vertex) =>
    {
        foreach (var behavior in vertex.Behaviors)
        {
            if (behavior.HasActionCode())
            {
                if (behavior.actionCode.Contains("$gil("))
                {
                    // keep actual code
                    behavior.actionCode += $"""console.log("Executed action: " + {EscapeFsmCode(behavior.actionCode)});""";
                }
                else
                {
                    // we don't want to execute the action, just log it.
                    behavior.actionCode = $"""console.log("FSM would execute action: " + {EscapeFsmCode(behavior.actionCode)});""";
                }
            }

            if (vertex is HistoryVertex)
            {
                if (behavior.HasGuardCode())
                {
                    // we want the history vertex to work as is without prompting the user to evaluate guard.
                    var logCode = $"""console.log("History state evaluating guard: " + {EscapeFsmCode(behavior.guardCode)})""";
                    var actualCode = behavior.guardCode;
                    behavior.guardCode = $"""{logCode} || {actualCode}""";
                }
                else
                {
                    behavior.actionCode += $"""console.log("History state taking default transition.");""";
                }
            }
            else
            {
                if (behavior.HasGuardCode())
                {
                    var logCode = $"""console.log("User evaluating guard: " + {EscapeFsmCode(behavior.guardCode)})""";
                    var confirmCode = $"""window.confirm("Evaluate guard: " + {EscapeFsmCode(behavior.guardCode)})""";
                    behavior.guardCode = $"""{logCode} || {confirmCode}""";
                    // NOTE! console logging doesn't return a value, so the confirm code will always be evaluated.
                }
            }
        }

        if (vertex is NamedVertex namedVertex)
        {
            namedVertex.AddEnterAction($"console.log(\"Entered {namedVertex.Name}.\");", index: 0);
            namedVertex.AddExitAction($"console.log(\"Exited {namedVertex.Name}.\");");
        }
    });
}

string EscapeFsmCode(string code)
{
    return "\"" + code.Replace("\"", "\\\"") + "\"";
}


public class LightSmRenderConfig : IRenderConfigC
{
    string IRenderConfigC.HFileTop => """
        extern void println(char const * str);
        extern void light_red();
        extern void light_blue();
        extern void light_yellow();
    """;
    string IRenderConfig.AutoExpandedVars => """
        int count = 0; // variable for state machine
        """;

    // This nested class creates expansions. It can have any name.
    public class MyExpansions : UserExpansionScriptBase
    {
        string count => AutoVarName(); // explained below
   }
}

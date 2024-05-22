#!/usr/bin/env dotnet-script
// This is a c# script file

#r "nuget: StateSmith, 0.9.13-alpha"

using StateSmith.Common;
using StateSmith.Input.Expansions;
using StateSmith.Output.UserConfig;
using StateSmith.Runner;
using StateSmith.SmGraph;  // Note using! This is required to access StateMachine and NamedVertex classes...

public StringBuilder imports = new();
public StringBuilder mocks = new();
public StringBuilder tests = new();

const string smName = "LightSm2"; // this would normally be detected, not hard coded.
const string simOutputDirectory = $"{smName}Sim";
private const string DiagramPath = "LightSm2.drawio.svg";

GenerateC99Code();
GenerateSimpleSimulator();

void GenerateC99Code()
{
    SmRunner runner = new(diagramPath: DiagramPath, new LightSmRenderConfig(), transpilerId: TranspilerId.C99);
    runner.Run();
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


///////////////// START OF CODE THAT COULD BE BUILT INTO STATE SMITH /////////////////


// this is pretty hacked together, but it's a start for prototyping the idea.

void GenerateSimpleSimulator()
{
    SmRunner runner = new(diagramPath: DiagramPath, transpilerId: TranspilerId.JavaScript, outputDirectory: simOutputDirectory);
    AddPipelineStep(runner);
    runner.Run();

    GenerateSimulationStandaloneFiles();
}

void GenerateSimulationStandaloneFiles()
{
    File.WriteAllText($"{PathUtils.GetThisDir()}/{simOutputDirectory}/index.html", $$"""
        <html>
        <body>
            Open web developer console to see the output of the code.

            <div id="buttons-div"></div>

            <script src="{{smName}}.js"></script>
            <script>
                const sm  = new {{smName}}();
                const buttonsDiv = document.getElementById("buttons-div");

                for (const eventName in {{smName}}.EventId) {
                    if (Object.hasOwnProperty.call({{smName}}.EventId, eventName)) {
                        const eventValue = {{smName}}.EventId[eventName];
                        const button = document.createElement("button");
                        button.innerText = eventName;
                        button.onclick = () => {
                            console.log(`=========== Dispatching event: ${eventName} ===========`);
                            sm.dispatchEvent(eventValue);
                        }
                        buttonsDiv.appendChild(button);
                    }
                }

                sm.start();
            </script>
        </body>
        </html>
        """
    );
}

void AddPipelineStep(SmRunner runner)
{
    // This method adds your custom step into the StateSmith transformation pipeline.
    // Some more info here: https://github.com/StateSmith/StateSmith/wiki/How-StateSmith-Works
    runner.SmTransformer.InsertBeforeFirstMatch(StandardSmTransformer.TransformationId.Standard_Validation1, 
                                                new TransformationStep(id: "my custom step blah", ModForSimulation));
}

// TODO - mod for simulation diagram when available. mermaid.js? cytoscape.js?
void ModForSimulation(StateMachine sm)
{
    sm.VisitRecursively((Vertex vertex) =>
    {
        foreach (var behavior in vertex.Behaviors)
        {
            ModBehaviorsForSimulation(vertex, behavior);
        }

        AddEntryExitTracing(vertex);
    });
}

static void AddEntryExitTracing(Vertex vertex)
{
    if (vertex is NamedVertex namedVertex)
    {
        namedVertex.AddEnterAction($"console.log(\"Entered {namedVertex.Name}.\");", index: 0);
        namedVertex.AddExitAction($"console.log(\"Exited {namedVertex.Name}.\");");
    }
}

void ModBehaviorsForSimulation(Vertex vertex, Behavior behavior)
{
    if (behavior.HasActionCode())
    {
        // GIL is Generic Intermediary Language. It is used by history vertices and other special cases.
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
            // we want the history vertex to work as is without prompting the user to evaluate those guards.
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

string EscapeFsmCode(string code)
{
    return "\"" + code.Replace("\"", "\\\"") + "\"";
}




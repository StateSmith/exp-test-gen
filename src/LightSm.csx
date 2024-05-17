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


var trackingExpander = new TrackingExpander();
SmRunner runner = new(diagramPath: "LightSm.drawio.svg", new LightSmRenderConfig(), transpilerId: TranspilerId.JavaScript);
runner.GetExperimentalAccess().DiServiceProvider.AddSingletonT<IExpander>(trackingExpander); // must be done before AddPipelineStep();
AddPipelineStep();
runner.Run();

foreach (var funcAttempt in trackingExpander.AttemptedFunctionExpansions)
{
    mocks.Append($"globalThis.{funcAttempt} = jest.fn();\n");
}

Console.WriteLine(imports.ToString());
Console.WriteLine(mocks.ToString());
Console.WriteLine(tests.ToString());


void AddPipelineStep()
{
    // This method adds your custom step into the StateSmith transformation pipeline.
    // Some more info here: https://github.com/StateSmith/StateSmith/wiki/How-StateSmith-Works
    runner.SmTransformer.InsertBeforeFirstMatch(StandardSmTransformer.TransformationId.Standard_Validation1, 
                                                new TransformationStep(id: "my custom step blah", PrintSmInfo));
}

// This shows roughly how to inspect a state machine.
// This same idea could be used to generate test scaffolding code or to generate documentation.
void PrintSmInfo(StateMachine sm)
{
    BehaviorDescriber describer = new(singleLineFormat: true);

    InitialState rootInitialState = sm.ChildType<InitialState>();

    // Imports    
    imports.Append($"import {{jest}} from '@jest/globals';\n");
    imports.Append($"import {{ {sm.Name} }} from './{sm.Name}.js';\n");

    // beforeEach
    tests.Append($"beforeEach(() => {{\n");
    tests.Append($"    jest.clearAllMocks();\n" );
    tests.Append($"}});\n");
    tests.Append("\n");

    // TODO this will not work in every case, but it's a start
    NamedVertex firstState = (NamedVertex)rootInitialState.TransitionBehaviors().Single().TransitionTarget;
    tests.Append($"test('starts in the {firstState.Name} state', () => {{\n");
    tests.Append($"    const sm = new {sm.Name}();\n");
    tests.Append($"    sm.start();\n");
    tests.Append($"    expect(sm.stateId).toBe({sm.Name}.StateId.{firstState.Name});\n" );
    tests.Append($"}});\n");
}


public class LightSmRenderConfig : IRenderConfigJavaScript
{
    bool IRenderConfigJavaScript.UseExportOnClass => true;

    string IRenderConfig.AutoExpandedVars => """
        count: 0 // variable for state machine
        """;

    
    // This nested class creates expansions. It can have any name.
    public class MyExpansions : UserExpansionScriptBase
    {
    }
}

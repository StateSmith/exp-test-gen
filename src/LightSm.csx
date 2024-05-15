#!/usr/bin/env dotnet-script
// This is a c# script file

#r "nuget: StateSmith, 0.9.12-alpha" // this line specifies which version of StateSmith to use and download from c# nuget web service.

using StateSmith.Input.Expansions;
using StateSmith.Output.UserConfig;
using StateSmith.Runner;
using StateSmith.SmGraph;  // Note using! This is required to access StateMachine and NamedVertex classes...

SmRunner runner = new(diagramPath: "LightSm.drawio.svg", new LightSmRenderConfig(), transpilerId: TranspilerId.JavaScript);
AddPipelineStep();
runner.Run();


/////////////////////////////////////////////////////////////////////////////////////////

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
    Console.WriteLine($"");
    BehaviorDescriber describer = new(singleLineFormat: true);

    InitialState rootInitialState = sm.ChildType<InitialState>();

    // Imports
    Console.WriteLine($"import {{jest}} from '@jest/globals';");
    Console.WriteLine($"import {{ {sm.Name} }} from './{sm.Name}.js';");
    Console.WriteLine($"");

    // beforeEach
    Console.WriteLine($"beforeEach(() => {{");
    Console.WriteLine($"    jest.clearAllMocks();" );
    Console.WriteLine($"}});");
    Console.WriteLine($"");

    // TODO is it a valid assumption to assume that every state machine has at least two vertices?
    // TODO is it a valid assumption to assume that the zeroth vertex is the entry vertex, and the 
    //      next vertex is the next reachable state from the vertex? Probably not.
    NamedVertex firstState = sm.GetNamedVerticesCopy()[1];
    Console.WriteLine($"test('starts in the {firstState.Name} state', () => {{");
    Console.WriteLine($"    const sm = new {sm.Name}();");
    Console.WriteLine($"    sm.start();");
    Console.WriteLine($"    expect(sm.stateId).toBe({sm.Name}.StateId.{firstState.Name});" );
    Console.WriteLine($"}});");

    Console.WriteLine($"");

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

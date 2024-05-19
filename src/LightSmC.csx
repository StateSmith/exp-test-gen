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
SmRunner runner = new(diagramPath: "LightSm.drawio.svg", new LightSmRenderConfig(), transpilerId: TranspilerId.C99);
runner.GetExperimentalAccess().DiServiceProvider.AddSingletonT<IExpander>(trackingExpander); // must be done before AddPipelineStep();
AddPipelineStep();
runner.Run();

foreach (var funcAttempt in trackingExpander.AttemptedFunctionExpansions)
{
    // We don't output mocks in the C version    
}

using(StreamWriter exampleTestFile = new StreamWriter("LightSm.example.test.c"))
{
    exampleTestFile.Write(imports.ToString());
    exampleTestFile.WriteLine();
    // exampleTestFile.Write(mocks.ToString());
    // exampleTestFile.WriteLine();
    exampleTestFile.Write(tests.ToString());
}



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
    imports.Append($"#include \"CppUTest/CommandLineTestRunner.h\"\n");
    imports.Append($"#include \"{sm.Name}.mocks.test.h\"\n");
    imports.Append($"#include \"{sm.Name}.h\"\n");

    // main
    tests.Append($"int main(int ac, char** av) {{\n");
    tests.Append($"    return CommandLineTestRunner::RunAllTests(ac, av);\n" );
    tests.Append($"}}\n");
    tests.Append("\n");

    // group
    tests.Append($"TEST_GROUP({sm.Name}Test) {{\n");
    tests.Append($"}};\n");
    tests.Append("\n");

    // TODO this will not work in every case, but it's a start
    NamedVertex firstState = (NamedVertex)rootInitialState.TransitionBehaviors().Single().TransitionTarget;
    tests.Append($"TEST({sm.Name}Test, StartsIn{firstState.Name}State) {{\n");
    tests.Append($"    {sm.Name} sm;\n");
    tests.Append($"    {sm.Name}_ctor(&sm);\n");
    tests.Append($"    {sm.Name}_start(&sm);\n");
    tests.Append($"    CHECK_EQUAL({sm.Name}_StateId_{firstState.Name}, sm.state_id);\n" );
    tests.Append($"}}\n");
    tests.Append("\n");
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

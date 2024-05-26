#!/usr/bin/env dotnet-script
// This is a c# script file

#r "nuget: StateSmith, 0.9.12-alpha" // this line specifies which version of StateSmith to use and download from c# nuget web service.

#nullable enable

using StateSmith.Input.Expansions;
using StateSmith.Output.UserConfig;
using StateSmith.Runner;
using StateSmith.SmGraph;  // Note using! This is required to access StateMachine and NamedVertex classes...
using StateSmith.SmGraph.Visitors;
using StateSmith.Common;
using System.Text.RegularExpressions;

// Mermaid code generation visits the graph first. It can record the id of each transition/edge.


public class EdgeOrderTracker
{
    Dictionary<Behavior, int> edgeIdMap = new();
    int nextId = 0;

    public int AddEdge(Behavior b)
    {
        int id = nextId;
        AdvanceId();
        edgeIdMap.Add(b, id);
        return id;
    }

    // use for when a non-behavior edge is added
    public int AdvanceId()
    {
        return nextId++;
    }

    public bool ContainsEdge(Behavior b)
    {
        return edgeIdMap.ContainsKey(b);
    }

    public int GetEdgeId(Behavior b)
    {
        return edgeIdMap[b];
    }
}



EdgeOrderTracker edgeOrderTracker = new();

TextWriter mermaidCodeWriter = new StringWriter();

// We need to run two transformation steps inside a single runner.
SmRunner jsRunner = new(diagramPath: "LightSm.drawio.svg", new LightSmRenderConfig(), transpilerId: TranspilerId.JavaScript);
jsRunner.Settings.propagateExceptions = true;
jsRunner.SmTransformer.InsertBeforeFirstMatch(StandardSmTransformer.TransformationId.Standard_RemoveNotesVertices, new TransformationStep(id: "some string id", GenerateMermaidCode));
jsRunner.SmTransformer.InsertBeforeFirstMatch(StandardSmTransformer.TransformationId.Standard_Validation1, new TransformationStep(id: "my custom step blah", LoggingTransformationStep));
jsRunner.Run();



void GenerateMermaidCode(StateMachine sm)
{
    var visitor2 = new MermaidVisitor2(edgeOrderTracker);
    visitor2.RenderAll(sm);
    mermaidCodeWriter.WriteLine(visitor2.GetMermaidCode());
    List<string> events = GetNonDoEvents(sm);
    events = events.ConvertAll(e => e.ToUpper()); // TODO what is the right way to get the event name in the proper case?
    StringWriter buttonCodeWriter = new StringWriter();
    StringWriter eventsCodeWriter = new StringWriter();
    foreach(var e in events) {
        buttonCodeWriter.WriteLine($"<button id=\"button_{e}\">{e}</button>"); // TODO will this handle special chars in event names?
        eventsCodeWriter.WriteLine($"document.getElementById('button_{e}').addEventListener('click', () => sm.dispatchEvent({sm.Name}.EventId.{e}), false);");
    }
    using(StreamWriter htmlWriter = new StreamWriter($"{sm.Name}.html")) {
        PrintHtml(htmlWriter,sm, buttonCodeWriter.ToString(), mermaidCodeWriter.ToString()!, eventsCodeWriter.ToString());
    }
}

void PrintHtml(TextWriter writer,  StateMachine sm, string buttonCode, string mermaidCode, string eventsCode) {

    string foo = $$"""
<html>
  <body>

    {{buttonCode}}

    <pre class="mermaid">
{{mermaidCode}}
    </pre>

    <script src="{{sm.Name}}.js"></script>
    <script type="module">
        import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs';
        mermaid.initialize({ startOnLoad: false });
        await mermaid.run();

        var sm = new {{sm.Name}}();

        {{eventsCode}}

        sm.start();
        // hack to show some transitions. Initial transition still not picked up yet.
        sm.dispatchEvent(LightSm.EventId.EV3);
        sm.dispatchEvent(LightSm.EventId.EV1);
        sm.dispatchEvent(LightSm.EventId.EV1);
        sm.dispatchEvent(LightSm.EventId.EV3);
    </script>
  </body>
</html>
""";

    writer.WriteLine(foo);
}



private List<string> GetNonDoEvents(StateMachine sm)
{
    var nonDoEvents = sm.GetEventListCopy();
    var ignored = nonDoEvents.RemoveAll((e) => TriggerHelper.IsDoEvent(e)) > 0;
    return nonDoEvents;
}


void LoggingTransformationStep(StateMachine sm)
{
    // The below code will visit all states in the state machine and add custom enter and exit behaviors.
    sm.VisitTypeRecursively<State>((State state) =>
    {
        state.AddEnterAction($"console.log(\"--> Entered {state.Name}.\");", index:0); // use index to insert at start
        state.AddExitAction($"console.log(\"<-- Exited {state.Name}.\");"); // behavior added to end

        // TODO how to handle escaping state names
        state.AddEnterAction($"document.querySelector('g[data-id={state.Name}]')?.classList.add('active');", index:0); // use index to insert at start
        state.AddExitAction($"document.querySelector('g[data-id={state.Name}]')?.classList.remove('active');");

        foreach (var b in state.TransitionBehaviors())
        {
            var domId = "edge" + edgeOrderTracker.GetEdgeId(b);
            // NOTE! Avoid single quotes in ss code until bug fixed: https://github.com/StateSmith/StateSmith/issues/282
            b.actionCode += $"""console.log(document.getElementById("{domId}"));""";
            b.actionCode += $"""document.getElementById("{domId}").style.stroke = "red";"""; // or something like this
        }
    });
}




class MermaidVisitor2 : IVertexVisitor
{
    int indentLevel = 0;
    StringBuilder sb = new();
    EdgeOrderTracker edgeOrderTracker;

    public MermaidVisitor2(EdgeOrderTracker edgeOrderTracker)
    {
        this.edgeOrderTracker = edgeOrderTracker;
    }

    public void RenderAll(StateMachine sm)
    {
        sm.Accept(this);
        RenderEdges(sm);
    }

    public string GetMermaidCode()
    {
        return sb.ToString();
    }

    public void Visit(StateMachine v)
    {
        AppendLn("stateDiagram");
        AppendLn("classDef active fill:yellow,stroke-width:2px;");
        indentLevel--; // don't indent the state machine contents
        VisitChildren(v);
    }

    public void Visit(State v)
    {
        if (v.Children.Count <= 0)
        {
            VisitLeafState(v);
        }
        else
        {
            VisitCompoundState(v);
        }
    }

    private void VisitCompoundState(State v)
    {
        AppendLn($$"""state {{v.Name}} {""");
        // FIXME - add behavior code here when supported by mermaid
        // https://github.com/StateSmith/StateSmith/issues/268#issuecomment-2111432194
        VisitChildren(v);
        AppendLn("}");
    }

    private void VisitLeafState(State v)
    {
        string name = v.Name;
        AppendLn(name);
        AppendLn($"{name} : {name}");
        foreach (var b in v.Behaviors.Where(b => b.TransitionTarget == null))
        {
            string text = b.ToString();
            text = MermaidEscape(text);
            AppendLn($"{name} : {text}");
        }
    }

    public void Visit(InitialState initialState)
    {
        string initialStateId = MakeVertexDiagramId(initialState);

        AppendLn($"[*] --> {initialStateId}");
        edgeOrderTracker.AdvanceId();  // we skip this "work around" edge for now. We can improve this later.
        AppendLn($"""state "$initial_state" as {initialStateId}""");
    }

    public void Visit(ChoicePoint v)
    {
        AppendLn($"""state {MakeVertexDiagramId(v)} <<choice>>""");
    }

    public void Visit(EntryPoint v)
    {
        AppendLn($"""state "$entry_pt.{v.label}" as {MakeVertexDiagramId(v)}""");
    }

    public void Visit(ExitPoint v)
    {
        AppendLn($"""state "$exit_pt.{v.label}" as {MakeVertexDiagramId(v)}""");
    }

    public void Visit(HistoryVertex v)
    {
        AppendLn($"""state "$H" as {MakeVertexDiagramId(v)}""");
    }

    public void Visit(HistoryContinueVertex v)
    {
        AppendLn($"""state "$HC" as {MakeVertexDiagramId(v)}""");
    }


    public void RenderEdges(StateMachine sm)
    {
        sm.VisitRecursively((Vertex v) =>
        {
            string vertexDiagramId = MakeVertexDiagramId(v);

            foreach (var behavior in v.Behaviors)
            {
                if (behavior.TransitionTarget != null)
                {
                    var text = behavior.ToString();
                    text = Regex.Replace(text, @"\s*TransitionTo[(].*[)]", ""); // bit of a hack to remove the `TransitionTo(SOME_STATE)` text
                    text = MermaidEscape(text);
                    sb.AppendLine($"{vertexDiagramId} --> {MakeVertexDiagramId(behavior.TransitionTarget)} : {text}");
                    edgeOrderTracker.AddEdge(behavior);
                }
            }
        });
    }

    public static string MakeVertexDiagramId(Vertex v)
    {
        switch (v)
        {
            case NamedVertex namedVertex:
                return namedVertex.Name;
            default:
                // see https://github.com/StateSmith/StateSmith/blob/04955e5df7d5eb6654a048dccb35d6402751e4c6/src/StateSmithTest/VertexDescribeTests.cs
                return Vertex.Describe(v).Replace("<", "(").Replace(">", ")");
        }
    }

    // TODO handle #
    // You can't naively add # to the list of characters because # and ; will interfere with each other
    private string MermaidEscape(string text) {
        foreach( char c in ";\\{}".ToCharArray()) {
            text = text.Replace(c.ToString(), $"#{(int)c};");
        }
        return text;
    }

    private void AppendLn(string message)
    {
        for (int i = 0; i < indentLevel; i++)
            sb.Append("        ");

        sb.AppendLine(message);
    }

    private void VisitChildren(Vertex v)
    {
        indentLevel++;
        foreach (var child in v.Children)
        {
            child.Accept(this);
        }
        indentLevel--;
    }

    public void Visit(RenderConfigVertex v)
    {
        // just ignore render config and any children
    }

    public void Visit(ConfigOptionVertex v)
    {
        // just ignore config option and any children
    }

    // orthogonal states are not yet implemented, but will be one day
    public void Visit(OrthoState v)
    {
        throw new NotImplementedException();
    }

    public void Visit(NotesVertex v)
    {
        // just ignore notes and any children
    }

    public void Visit(NamedVertex v)
    {
        throw new NotImplementedException(); // should not be called
    }

    public void Visit(Vertex v)
    {
        throw new NotImplementedException(); // should not be called
    }
}




// This class gives StateSmith the info it needs to generate working C code.
// It adds user code to the generated .c/.h files, declares user variables,
// and provides diagram code expansions. This class can have any name.
public class LightSmRenderConfig : IRenderConfigJavaScript
{

    string IRenderConfig.AutoExpandedVars => """
        count: 0 // variable for state machine
        """;


    // This nested class creates expansions. It can have any name.
    public class MyExpansions : UserExpansionScriptBase
    {
        // public string light_blue()   => """std::cout << "BLUE\n";""";
        // public string light_yellow() => """std::cout << "YELL-OH\n";""";
        // public string light_red()    => """std::cout << "RED\n";""";
    }
}


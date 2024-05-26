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

TextWriter mermaidCodeWriter = new StringWriter();
SmRunner htmlRunner = new(diagramPath: "LightSm.drawio.svg", new LightSmRenderConfig(), transpilerId: TranspilerId.JavaScript);
htmlRunner.SmTransformer.InsertBeforeFirstMatch(
    StandardSmTransformer.TransformationId.Standard_RemoveNotesVertices,
    new TransformationStep(id: "some string id", action: (sm) =>
    {
        // var visitor = new MermaidGenerator(mermaidCodeWriter);
        var visitor2 = new MermaidVisitor2();
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
    }));
htmlRunner.Run();


// HACK order is important, the jsRunner must run after the htmlRunner, because the htmlRunner
// also generate js (but without the logging transforms), and the jsRunner must be the last to write
SmRunner jsRunner = new(diagramPath: "LightSm.drawio.svg", new LightSmRenderConfig(), transpilerId: TranspilerId.JavaScript);
jsRunner.SmTransformer.InsertBeforeFirstMatch(StandardSmTransformer.TransformationId.Standard_Validation1, 
                                            new TransformationStep(id: "my custom step blah", LoggingTransformationStep));
jsRunner.Run();



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
    });
}




class MermaidVisitor2 : IVertexVisitor
{
    int indentLevel = 0;
    StringBuilder sb = new();

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



// TODO it might be more straightforward to iterate over the graph directly instead of using a visitor
class MermaidGenerator : IVertexVisitor
{
    private HashSet<Vertex> leafNodes = new HashSet<Vertex>();
    private HashSet<Vertex> compositeNodes = new HashSet<Vertex>();      
    private TextWriter writer;

    public MermaidGenerator(TextWriter writer)
    {
        this.writer = writer;
    }


    // Format for regular state:
    //   OFF : title
    //   OFF : first line
    //   OFF : second line
    //
    // Format for composite state (multiple lines not supported):
    //   state OFF {
    //    ...
    //   }
    //
    // Transitions must be first
    // Then regular states
    // Then composite states
    //
    // At least that the order that seems to be working best on my test models
    // https://github.com/mermaid-js/mermaid/issues/5522
    public void Print() {
        Print("stateDiagram");
        Print("classDef active fill:yellow,stroke-width:2px;");
        Print("");
        foreach (var node in leafNodes.Concat(compositeNodes)) {
            PrintTransitions(node);
        }
        foreach (var node in leafNodes) {
            PrintLeafNode(node);
        }
        foreach (var node in compositeNodes) {
            PrintCompositeNode(node);
        }
    }


    private void PrintLeafNode(Vertex v) {
        if( v is NamedVertex ) {
            string name = ((NamedVertex)v).Name;
            Print($"{name} : {name}");
            foreach(var b in v.Behaviors.Where(b => b.TransitionTarget==null)) {
                string text = MermaidEscape(b.ToString());
                Print($"{name} : {text}");
            }
            Print("");
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

    private void PrintCompositeNode(Vertex v) {
        if( !(v is NamedVertex) ) {
            throw new Exception("Composite node must be named");
        }

        Print($"state {((NamedVertex)v).Name} {{");
        foreach (var child in v.Children)
        {
            if(child is NamedVertex) {
                Print(((NamedVertex)child).Name);
            }
        }
        Print("}");
        Print("");
    }

    private void PrintTransitions(Vertex v) {
        foreach (var behavior in v.Behaviors)
        {
            if(behavior.TransitionTarget!=null) {
                string start = v is NamedVertex ? ((NamedVertex)v).Name : "[*]";
                string end = behavior.TransitionTarget is NamedVertex ? ((NamedVertex)behavior.TransitionTarget).Name : "[*]";
                Print($"{start} --> {end}");
            }
        }
        Print("");
    }

    private void Print(string message)
    {
        writer.WriteLine(message);
    }

    private void VisitChildren(Vertex v)
    {
        foreach (var child in v.Children)
        {
            child.Accept(this);
        }
    }

    private void AssertNoChildren(Vertex v)
    {
        if (v.Children.Count > 0)
        {
            throw new Exception($"Vertex `{Vertex.Describe(v)}` not expected to have children");
        }
    }

    public void Visit(Vertex v)
    {
        throw new NotImplementedException();
    }

    public void Visit(StateMachine v)
    {
        VisitChildren(v);
    }

    public void Visit(NamedVertex v)
    {
        VisitChildren(v);
    }

    public void Visit(State v)
    {
        if(v.Children.Count > 0) {
            compositeNodes.Add(v);
            VisitChildren(v);
        } else {
            leafNodes.Add(v);
        }
    }

    // orthogonal states are not yet implemented, but will be one day
    public void Visit(OrthoState v)
    {
        throw new NotImplementedException();
    }

    public void Visit(NotesVertex v)
    {
        throw new NotImplementedException();
    }

    public void Visit(InitialState v)
    {
        AssertNoChildren(v);
    }

    public void Visit(ChoicePoint v)
    {
        throw new NotImplementedException();
    }

    public void Visit(EntryPoint v)
    {
        throw new NotImplementedException();
    }

    public void Visit(ExitPoint v)
    {
        throw new NotImplementedException();
    }

    public void Visit(HistoryVertex v)
    {
        throw new NotImplementedException();
    }

    public void Visit(HistoryContinueVertex v)
    {
        throw new NotImplementedException();
    }

    public void Visit(RenderConfigVertex v)
    {
        // just ignore render config and any children
    }

    public void Visit(ConfigOptionVertex v)
    {
        // just ignore config option and any children
    }

    private void VisitBehaviors(Vertex v)
    {
        foreach (var behavior in v.Behaviors)
        {
            if(behavior.TransitionTarget!=null) {
                string start = v is NamedVertex ? ((NamedVertex)v).Name : "[*]";
                string end = behavior.TransitionTarget is NamedVertex ? ((NamedVertex)behavior.TransitionTarget).Name : "[*]";
                
                Print($"{start} --> {end}");
            }
        }
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


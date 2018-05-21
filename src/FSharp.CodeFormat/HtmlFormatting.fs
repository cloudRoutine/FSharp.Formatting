// --------------------------------------------------------------------------------------
// F# CodeFormat (HtmlFormatting.fs)
// (c) Tomas Petricek, 2012, Available under Apache 2.0 license.
// --------------------------------------------------------------------------------------

module internal FSharp.CodeFormat.Html

open System
open System.IO
open System.Web
open System.Text
open System.Collections.Generic
open FSharp.CodeFormat
open FSharp.CodeFormat.Constants

// --------------------------------------------------------------------------------------
// Context used by the formatter
// --------------------------------------------------------------------------------------

/// Represents context used by the formatter
type HtmlFormattingContext = { 
    AddLines       : bool
    GenerateErrors : bool
    TextBuffer     : StringBuilder
    OpenTag        : string
    CloseTag       : string
    OpenLinesTag   : string
    CloseLinesTag  : string
    FormatTip      : ToolTipSpan list -> bool -> (ToolTipSpan list -> string) -> string 
}


/// Mutable type that formats tool tips and keeps the generated HTML
type HtmlToolTipFormatter (prefix) = 
    let tips = new Dictionary<ToolTipSpan list, int * string>()
    let mutable count = 0
    let mutable uniqueId = 0

    /// Formats tip and returns assignments for 'onmouseover' and 'onmouseout'
    member __.FormatTip (tip:ToolTipSpan list) overlapping formatFunction = 
        uniqueId <- uniqueId + 1
        let stringIndex =
            match tips.TryGetValue tip with
            | true, (idx, _) -> idx
            | _ -> 
            count <- count + 1
            tips.Add (tip, (count, formatFunction tip))
            count
        // stringIndex is the index of the tool tip
        // uniqueId is globally unique id of the occurrence
        if overlapping then
            // The <span> may contain other <span>, so we need to 
            // get the element and check where the mouse goes...
            String.Format (   
                "id=\"{0}t{1}\" onmouseout=\"hideTip(event, '{0}{1}', {2})\" " + 
                "onmouseover=\"showTip(event, '{0}{1}', {2}, document.getElementById('{0}t{1}'))\" ",
                prefix, stringIndex, uniqueId 
            )
        else
            String.Format(
                "onmouseout=\"hideTip(event, '{0}{1}', {2})\" " + 
                "onmouseover=\"showTip(event, '{0}{1}', {2})\" ",
                prefix, stringIndex, uniqueId 
            )
  

    /// Returns all generated tool tip elements
    member __.WriteTipElements (writer:StringBuilder) = 
        for (KeyValue (_, (index, html))) in tips do
            writer.AppendLine(sprintf "<div class=\"tip\" id=\"%s%d\">%s</div>" prefix index html) |> ignore



// --------------------------------------------------------------------------------------
// Formats various types from 'SourceCode.fs' as HTML
// --------------------------------------------------------------------------------------

/// Formats tool tip information and returns a string
let formatToolTipSpans (spans:ToolTipSpan list) = 
    let sb = StringBuilder ()
    use wr = new StringWriter(sb)
    // Inner recursive function that does the formatting
    let rec format spans = spans |> List.iter (function
        | Emphasis spans ->
            wr.Write "<em>"
            format spans
            wr.Write "</em>"
        | Literal string ->
            let spaces = string.Length - string.TrimStart(' ').Length
            wr.Write (String.replicate spaces "&#160;")
            wr.Write (HttpUtility.HtmlEncode(string.Substring spaces))
        | HardLineBreak ->
            wr.Write "<br />"
    )
    format spans
    sb.ToString ()

/// Format token spans such as tokens, omitted code etc.
let rec formatTokenSpans (ctx:HtmlFormattingContext) = List.iter (function
    | Error (_kind, message, body) when ctx.GenerateErrors ->
        let tip = ToolTipReader.formatMultilineString (message.Trim())
        let tipAttributes = ctx.FormatTip tip true formatToolTipSpans
        ctx.TextBuffer {
            append "<span "
            append tipAttributes
            append "class=\"cerr\">"
        } |> ignore
        formatTokenSpans { ctx with FormatTip = fun _ _ _ -> "" } body
        ctx.TextBuffer.Append "</span>"  |> ignore
    | Error (_, _, body) ->
        formatTokenSpans ctx body
    | Output body ->
        ctx.TextBuffer {
            append "<span class=\"fsi\">"
            append (HttpUtility.HtmlEncode body)
            append "</span>"
        } |> ignore
    | Omitted(body, hidden) ->
        let tip = ToolTipReader.formatMultilineString (hidden.Trim())
        let tipAttributes = ctx.FormatTip tip true formatToolTipSpans
        ctx.TextBuffer {
            append "<span "
            append "<span "
            append tipAttributes
            append "class=\"omitted\">"
            append body
            append "</span>"
        } |> ignore
    | Token (kind, body, tip) ->
        // Generate additional attributes for ToolTip
        let tipAttributes = 
            match tip with
            | Some tip -> ctx.FormatTip tip false formatToolTipSpans
            | _ -> ""

        if kind <> TokenKind.Default then
            // Colorize token & add tool tip
            ctx.TextBuffer {
                append "<span "
                append tipAttributes
                append ("class=\"" + kind.Color + "\">")
                append (HttpUtility.HtmlEncode body)
                append "</span>"
            } |> ignore
        else       
            ctx.TextBuffer.Append (HttpUtility.HtmlEncode body)|> ignore
)


/// Generate HTML with the specified snippets
let formatSnippets (ctx:HtmlFormattingContext) (snippets:Snippet[]) = [|
    for (Snippet(title, lines)) in snippets do
        // Skip empty lines at the beginning and at the end
        let skipEmptyLines =
            Seq.skipWhile (fun (Line spans) -> List.isEmpty spans) >> List.ofSeq
        let lines =
            lines |> skipEmptyLines |> List.rev |> skipEmptyLines |> List.rev

        // Generate snippet to a local StringBuilder
        let mainStr = StringBuilder()
        let ctx = { ctx with TextBuffer = mainStr }

        let numberLength = lines.Length.ToString().Length
        let linesLength = lines.Length
        let emitTag tag = 
            if String.IsNullOrEmpty tag |> not then 
                ctx.TextBuffer.Append tag |> ignore

        // If we're adding lines, then generate two column table 
        // (so that the body can be easily copied)
        if ctx.AddLines then
            ctx.TextBuffer {
                append "<table class=\"pre\">"
                append "<tr>"
                append "<td class=\"lines\">"
            } |> ignore

            // Generate <pre> tag for the snippet
            emitTag ctx.OpenLinesTag
            // Print all line numbers of the snippet
            for index in 0..linesLength-1 do
                // Add line number to the beginning
                let lineStr = (index + 1).ToString().PadLeft(numberLength)
                ctx.TextBuffer.AppendFormat("<span class=\"l\">{0}: </span>", lineStr) |> ignore

            emitTag ctx.CloseLinesTag
            ctx.TextBuffer {
                appendLine "</td>"
                append "<td class=\"snippet\">"
            } |> ignore

        // Print all lines of the snippet inside <pre>..</pre>
        emitTag ctx.OpenTag
        lines |> List.iter (fun (Line spans) ->
            formatTokenSpans ctx spans
            ctx.TextBuffer.AppendLine() |> ignore
        )
        emitTag ctx.CloseTag

        if ctx.AddLines then
            // Close the table if we are adding lines
            ctx.TextBuffer {
                appendLine "</td>"
                appendLine "</tr>"
                append "</table>"
            } |> ignore
        yield title, mainStr.ToString()
|]

/// Format snippets and return HTML for <pre> tags together
/// wtih HTML for ToolTips (to be added to the end of document)
let format addLines addErrors prefix openTag closeTag openLinesTag closeLinesTag (snippets:Snippet[]) = 
    let tipf = HtmlToolTipFormatter prefix
    let ctx =  { 
        AddLines       = addLines 
        GenerateErrors = addErrors
        TextBuffer     = StringBuilder()
        FormatTip      = tipf.FormatTip 
        OpenLinesTag   = openLinesTag
        CloseLinesTag  = closeLinesTag
        OpenTag        = openTag 
        CloseTag       = closeTag 
    }
    // Generate main HTML for snippets
    let snippets = formatSnippets ctx snippets
    // Generate HTML with ToolTip tags
    let tipStr = StringBuilder()
    tipf.WriteTipElements( tipStr )
    snippets, tipStr.ToString()

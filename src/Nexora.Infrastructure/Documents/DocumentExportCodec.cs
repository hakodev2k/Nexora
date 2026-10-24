using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Nexora.Infrastructure.Documents;

internal sealed record DocumentExportConversion(byte[] Bytes,string[] Warnings);
internal sealed class DocumentConversionException(string code):Exception(code);

internal static class DocumentExportCodec
{
    internal const int MaxOutputBytes=5*1024*1024;
    private static readonly MarkdownPipeline Pipeline=new MarkdownPipelineBuilder{MaximumNestingDepth=64}.UsePipeTables().UseTaskLists().UseEmphasisExtras(EmphasisExtraOptions.Strikethrough).Build();
    internal static DocumentExportConversion Convert(DocumentExportSnapshot source,string format)
    {
        var warnings=new HashSet<string>(StringComparer.Ordinal);
        var markdown=source.Mode=="Markdown"?source.Body:BlockMarkdown(source.Body,warnings);
        if(format=="MD")InspectMarkdown(markdown,warnings);
        byte[] bytes=format switch{"MD"=>new UTF8Encoding(false,true).GetBytes(markdown),"DOCX"=>new WordWriter(markdown,warnings).Write(),_=>throw new DocumentConversionException("UnsupportedFormat")};
        if(bytes.Length>MaxOutputBytes)throw new DocumentConversionException("OutputLimitExceeded");
        return new(bytes,warnings.Order(StringComparer.Ordinal).ToArray());
    }
    internal static void InspectMarkdown(string markdown,HashSet<string> warnings)
    {
        var count=0;
        void VisitInline(ContainerInline inline,int depth)
        {
            if(depth>64)throw new DocumentConversionException("ConversionLimitExceeded");
            foreach(var child in inline)
            {
                if(++count>10000)throw new DocumentConversionException("ConversionLimitExceeded");
                if(child is LinkInline{IsImage:true})warnings.Add("ImageReferencesNotEmbedded");
                if(child is LinkInline{IsImage:false} or AutolinkInline)warnings.Add("LinksRemainLiteralSource");
                if(child is HtmlInline)warnings.Add("HtmlRemainsLiteralSource");
                if(child is ContainerInline nested)VisitInline(nested,depth+1);
            }
        }
        void Visit(ContainerBlock block,int depth)
        {
            if(depth>64)throw new DocumentConversionException("ConversionLimitExceeded");
            foreach(var child in block){if(++count>10000)throw new DocumentConversionException("ConversionLimitExceeded");if(child is HtmlBlock)warnings.Add("HtmlRemainsLiteralSource");if(child is ContainerBlock nested)Visit(nested,depth+1);if(child is LeafBlock{Inline:{} inline})VisitInline(inline,depth+1);}
        }
        Visit(Markdown.Parse(markdown,Pipeline),0);
    }
    private static string Escape(string text)=>Regex.Replace(text,@"([\\`*_{}\[\]()#+\-.!|>~:])",@"\$1");
    private static string Literal(string text)=>string.Join("  \n",text.Replace("\r\n","\n",StringComparison.Ordinal).Split('\n').Select(Escape));
    private static string BlockMarkdown(string body,HashSet<string> warnings)
    {
        if(body.Length==0)return string.Empty;
        if(!DocumentBlockSchema.IsValid(body))
        {
            if(!DocumentBlockSchema.IsLegacyText(body))throw new DocumentConversionException("UnsupportedBlockSchema");
            warnings.Add("LegacyBlockText");return Literal(body);
        }
        using var json=JsonDocument.Parse(body);var output=new StringBuilder();
        foreach(var block in json.RootElement.GetProperty("blocks").EnumerateArray())
        {
            string Text(string name="text")=>block.GetProperty(name).GetString()!;
            switch(Text("type"))
            {
                case "paragraph":if(string.IsNullOrWhiteSpace(Text()))warnings.Add("EmptyBlockSpacingNormalized");output.Append(Literal(Text()));break;
                case "heading":output.Append(new string('#',block.GetProperty("level").GetInt32())).Append(' ').Append(Literal(Text()));break;
                case "quote":foreach(var line in Text().Replace("\r\n","\n",StringComparison.Ordinal).Split('\n'))output.Append("> ").Append(Escape(line)).Append('\n');break;
                case "divider":output.Append("---");break;
                case "link":warnings.Add("ExternalLinksInert");output.Append(Literal(Text())).Append(" (").Append(Escape(Text("url"))).Append(')');break;
                case "code":
                    var code=Text();var fence=new string('`',Math.Max(3,Regex.Matches(code,"`+").Select(match=>match.Length+1).DefaultIfEmpty(3).Max()));
                    var language=block.TryGetProperty("language",out var lang)?lang.GetString()??"":"";
                    if(!Regex.IsMatch(language,@"\A[A-Za-z0-9_+#.\-]{0,64}\z")){language="";warnings.Add("CodeLanguageOmitted");}
                    var addNewline=code.Length>0&&!code.EndsWith('\n');if(addNewline)warnings.Add("CodeTrailingNewlineAdded");
                    output.Append(fence).Append(language).Append('\n').Append(code).Append(addNewline?"\n":"").Append(fence);break;
                case "list":
                    var ordered=block.GetProperty("ordered").GetBoolean();var index=0;
                    foreach(var item in block.GetProperty("items").EnumerateArray()){var prefix=ordered?$"{++index}. ":"- ";output.Append(prefix).Append(Literal(item.GetString()!).Replace("\n","\n"+new string(' ',prefix.Length),StringComparison.Ordinal)).Append('\n');}break;
                case "checklist":foreach(var item in block.GetProperty("items").EnumerateArray())output.Append(item.GetProperty("checked").GetBoolean()?"- [x] ":"- [ ] ").Append(Literal(item.GetProperty("text").GetString()!).Replace("\n","\n      ",StringComparison.Ordinal)).Append('\n');break;
                case "table":
                    warnings.Add("TableFirstRowBecomesHeader");var rowIndex=0;
                    foreach(var row in block.GetProperty("rows").EnumerateArray())
                    {
                        output.Append('|');foreach(var cell in row.EnumerateArray()){var value=cell.GetString()!;if(value.Contains('\n')||value.Contains('\r'))warnings.Add("TableCellLineBreaksFlattened");output.Append(' ').Append(Escape(value.Replace('\r',' ').Replace('\n',' '))).Append(" |");}output.Append('\n');
                        if(rowIndex++==0)output.Append('|').Append(string.Join('|',Enumerable.Repeat(" --- ",row.GetArrayLength()))).Append("|\n");
                    }break;
                default:throw new DocumentConversionException("UnsupportedBlockSchema");
            }
            output.Append("\n\n");
            if(output.Length>MaxOutputBytes)throw new DocumentConversionException("OutputLimitExceeded");
        }
        return output.ToString();
    }

    private sealed class WordWriter(string markdown,HashSet<string> warnings)
    {
        private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XNamespace R="http://schemas.openxmlformats.org/package/2006/relationships";
        private readonly List<XElement> numbering=[];
        private int nodes;
        private void Budget(int depth){if(++nodes>10000||depth>64)throw new DocumentConversionException("ConversionLimitExceeded");}
        private string XmlText(string text)
        {
            var output=new StringBuilder(text.Length);foreach(var rune in text.EnumerateRunes())
                if(rune.Value is 9 or 10 or 13 or >=32 and <=0xD7FF or >=0xE000 and <=0xFFFD or >=0x10000 and <=0x10FFFF)output.Append(rune.ToString());else warnings.Add("XmlCharactersOmitted");
            return output.ToString();
        }
        private XElement Run(string text,bool bold=false,bool italic=false,bool strike=false,bool code=false)
        {
            var properties=new XElement(W+"rPr",bold?new XElement(W+"b"):null,italic?new XElement(W+"i"):null,strike?new XElement(W+"strike"):null,code?new XElement(W+"rFonts",new XAttribute(W+"ascii","Consolas"),new XAttribute(W+"hAnsi","Consolas")):null);
            var run=new XElement(W+"r",properties);var lines=XmlText(text).Replace("\r\n","\n",StringComparison.Ordinal).Split('\n');
            for(var i=0;i<lines.Length;i++){if(i>0)run.Add(new XElement(W+"br"));run.Add(new XElement(W+"t",new XAttribute(XNamespace.Xml+"space","preserve"),lines[i]));}return run;
        }
        private IEnumerable<XElement> Inlines(ContainerInline? container,int depth,bool bold=false,bool italic=false,bool strike=false)
        {
            if(container is null)yield break;
            foreach(var inline in container){Budget(depth);switch(inline)
            {
                case LiteralInline literal:yield return Run(literal.Content.ToString(),bold,italic,strike);break;
                case CodeInline code:yield return Run(code.Content,bold,italic,strike,true);break;
                case LineBreakInline lineBreak:yield return lineBreak.IsHard?new XElement(W+"r",new XElement(W+"br")):Run(" ",bold,italic,strike);break;
                case HtmlEntityInline entity:yield return Run(entity.Transcoded.ToString(),bold,italic,strike);break;
                case HtmlInline html:warnings.Add("HtmlShownLiterally");yield return Run(html.Tag,bold,italic,strike);break;
                case EmphasisInline emphasis:foreach(var run in Inlines(emphasis,depth+1,bold||emphasis.DelimiterChar!='~'&&emphasis.DelimiterCount==2,italic||emphasis.DelimiterChar!='~'&&emphasis.DelimiterCount==1,strike||emphasis.DelimiterChar=='~'))yield return run;break;
                case LinkInline link:
                    warnings.Add(link.IsImage?"ImagesOmitted":"ExternalLinksInert");
                    if(link.IsImage)yield return Run("[Image omitted] ");
                    foreach(var run in Inlines(link,depth+1,bold,italic,strike))yield return run;
                    if(!link.IsImage&&link.Url is {} url)yield return Run(" ("+url+")",bold,italic,strike);break;
                case AutolinkInline auto:warnings.Add("ExternalLinksInert");yield return Run(auto.Url,bold,italic,strike);break;
                case TaskList task:warnings.Add("ChecklistIsStatic");yield return Run(task.Checked?"☑ ":"☐ ");break;
                case ContainerInline nested:warnings.Add("UnsupportedInlineFlattened");foreach(var run in Inlines(nested,depth+1,bold,italic,strike))yield return run;break;
                default:warnings.Add("UnsupportedInlineOmitted");break;
            }}
        }
        private XElement Paragraph(IEnumerable<XElement> runs,string? style=null,int? number=null,int level=0)
            =>new(W+"p",new XElement(W+"pPr",style is null?null:new XElement(W+"pStyle",new XAttribute(W+"val",style)),number is null?null:new XElement(W+"numPr",new XElement(W+"ilvl",new XAttribute(W+"val",Math.Min(level,8))),new XElement(W+"numId",new XAttribute(W+"val",number)))),runs);
        private int Numbering(ListBlock list,int level)
        {
            var id=numbering.Count+1;var valid=int.TryParse(list.OrderedStart,out var parsed)&&parsed>=0;var start=valid?parsed:1;if(list.IsOrdered&&!valid)warnings.Add("OrderedListStartNormalized");
            numbering.Add(new XElement(W+"num",new XAttribute(W+"numId",id),new XElement(W+"abstractNumId",new XAttribute(W+"val",list.IsOrdered?1:0)),new XElement(W+"lvlOverride",new XAttribute(W+"ilvl",Math.Min(level,8)),new XElement(W+"startOverride",new XAttribute(W+"val",start)))));return id;
        }
        private IEnumerable<XElement> Blocks(ContainerBlock container,int depth=0,int? number=null,int level=0,bool quote=false)
        {
            var listContext=number.HasValue;
            foreach(var block in container)
            {
                Budget(depth);switch(block)
                {
                    case HeadingBlock heading:yield return Paragraph(Inlines(heading.Inline,depth+1),"Heading"+heading.Level,number,level);number=null;break;
                    case ParagraphBlock paragraph:yield return Paragraph(Inlines(paragraph.Inline,depth+1),quote?"Quote":null,number,level);number=null;break;
                    case CodeBlock code:yield return Paragraph([Run(code.Lines.ToString(),code:true)],"Code",number,level);number=null;break;
                    case ThematicBreakBlock:yield return Paragraph([Run("────────")]);break;
                    case ListBlock list:
                        var nextLevel=listContext?level+1:level;if(nextLevel>8)warnings.Add("DeepListIndentFlattened");var id=Numbering(list,nextLevel);
                        foreach(var item in list.OfType<ListItemBlock>())foreach(var element in Blocks(item,depth+1,id,nextLevel,quote))yield return element;break;
                    case QuoteBlock quotation:if(quote)warnings.Add("NestedQuoteIndentFlattened");foreach(var element in Blocks(quotation,depth+1,number,level,true))yield return element;break;
                    case Table table:
                        warnings.Add("TableLayoutNormalized");
                        var output=new XElement(W+"tbl",new XElement(W+"tblPr",new XElement(W+"tblW",new XAttribute(W+"w",0),new XAttribute(W+"type","auto"))));
                        foreach(var row in table.OfType<TableRow>())
                        {Budget(depth+1);var tr=new XElement(W+"tr",row.IsHeader?new XElement(W+"trPr",new XElement(W+"tblHeader")):null);foreach(var cell in row.OfType<TableCell>()){Budget(depth+2);var content=Blocks(cell,depth+3).ToList();if(content.Count==0)content.Add(Paragraph([]));tr.Add(new XElement(W+"tc",content));}output.Add(tr);}yield return output;break;
                    case LeafBlock leaf:
                        warnings.Add("UnsupportedBlockShownLiterally");yield return Paragraph([Run(leaf.Lines.ToString())]);break;
                    case ContainerBlock nested:warnings.Add("UnsupportedContainerFlattened");foreach(var element in Blocks(nested,depth+1,number,level,quote))yield return element;break;
                    default:warnings.Add("UnsupportedBlockOmitted");break;
                }
            }
        }
        internal byte[] Write()
        {
            var ast=Markdown.Parse(markdown,Pipeline);var body=new XElement(W+"body",Blocks(ast));body.Add(new XElement(W+"sectPr"));
            using var output=new MemoryStream();using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
            {
                var contentNamespace=(XNamespace)"http://schemas.openxmlformats.org/package/2006/content-types";
                Add(zip,"[Content_Types].xml",new XElement(contentNamespace+"Types",new XElement(contentNamespace+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(contentNamespace+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),new XElement(contentNamespace+"Override",new XAttribute("PartName","/word/document.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml")),new XElement(contentNamespace+"Override",new XAttribute("PartName","/word/styles.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml")),new XElement(contentNamespace+"Override",new XAttribute("PartName","/word/numbering.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml"))));
                Add(zip,"_rels/.rels",new XElement(R+"Relationships",Relation("rId1","officeDocument","word/document.xml")));
                Add(zip,"word/document.xml",new XElement(W+"document",new XAttribute(XNamespace.Xmlns+"w",W),body));
                Add(zip,"word/_rels/document.xml.rels",new XElement(R+"Relationships",Relation("rId1","styles","styles.xml"),Relation("rId2","numbering","numbering.xml")));
                var styles=new XElement(W+"styles",new XAttribute(XNamespace.Xmlns+"w",W));
                foreach(var name in Enumerable.Range(1,6).Select(i=>"Heading"+i).Concat(["Quote","Code"]))
                {
                    var heading=name.StartsWith("Heading",StringComparison.Ordinal);var level=heading?int.Parse(name.AsSpan(7)):0;
                    styles.Add(new XElement(W+"style",new XAttribute(W+"type","paragraph"),new XAttribute(W+"styleId",name),new XElement(W+"name",new XAttribute(W+"val",name)),
                        new XElement(W+"pPr",heading?new XElement(W+"outlineLvl",new XAttribute(W+"val",level-1)):name=="Quote"?new XElement(W+"ind",new XAttribute(W+"left",720)):null),
                        new XElement(W+"rPr",heading?new XElement(W+"b"):null,heading?new XElement(W+"sz",new XAttribute(W+"val",36-level*2)):null)));
                }
                Add(zip,"word/styles.xml",styles);
                var numbers=new XElement(W+"numbering",new XAttribute(XNamespace.Xmlns+"w",W));
                for(var kind=0;kind<2;kind++)numbers.Add(new XElement(W+"abstractNum",new XAttribute(W+"abstractNumId",kind),new XElement(W+"multiLevelType",new XAttribute(W+"val","multilevel")),Enumerable.Range(0,9).Select(i=>new XElement(W+"lvl",new XAttribute(W+"ilvl",i),new XElement(W+"start",new XAttribute(W+"val",1)),new XElement(W+"numFmt",new XAttribute(W+"val",kind==1?"decimal":"bullet")),new XElement(W+"lvlText",new XAttribute(W+"val",kind==1?$"%{i+1}.":"•"))))));
                numbers.Add(numbering);Add(zip,"word/numbering.xml",numbers);
            }
            if(output.Length>MaxOutputBytes)throw new DocumentConversionException("OutputLimitExceeded");return output.ToArray();
        }
        private static XElement Relation(string id,string type,string target)=>new(R+"Relationship",new XAttribute("Id",id),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/"+type),new XAttribute("Target",target));
        private static void Add(ZipArchive zip,string path,XElement element)
        {
            var entry=zip.CreateEntry(path,CompressionLevel.Optimal);entry.LastWriteTime=new DateTimeOffset(1980,1,1,0,0,0,TimeSpan.Zero);
            using var stream=entry.Open();using var writer=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false),Indent=false});new XDocument(element).Save(writer);
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Nexora.Infrastructure.Documents;

internal static partial class DocumentImportCodec
{
    private static readonly MarkdownPipeline ImportPipeline=new MarkdownPipelineBuilder{MaximumNestingDepth=64}.UsePipeTables().UseTaskLists().UseEmphasisExtras(EmphasisExtraOptions.Strikethrough).Build();
    private static string ToBlock(string markdown,HashSet<string> warnings)=>new BlockReader(markdown,warnings).Read();
    private sealed class BlockReader(string markdown,HashSet<string> warnings)
    {
        private readonly List<Dictionary<string,object>> blocks=[];
        private readonly string seed=System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(markdown)));
        private int nodes;
        private void Budget(int depth){if(++nodes>10000||depth>64)throw new DocumentConversionException("ConversionLimitExceeded");}
        private void Add(string type,params (string Key,object Value)[] fields)
        {
            if(blocks.Count>=1000)throw new DocumentConversionException("BlockLimitExceeded");
            var bytes=SHA256.HashData(Encoding.UTF8.GetBytes(seed+":"+blocks.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));bytes[6]=(byte)((bytes[6]&15)|64);bytes[8]=(byte)((bytes[8]&63)|128);
            var block=new Dictionary<string,object>(StringComparer.Ordinal){["id"]=new Guid(bytes.AsSpan(0,16)).ToString("D"),["type"]=type};foreach(var field in fields)block[field.Key]=field.Value;blocks.Add(block);
        }
        private string Inline(ContainerInline? container,int depth)
        {
            if(container is null)return "";var text=new StringBuilder();
            foreach(var child in container)
            {
                Budget(depth);switch(child)
                {
                    case LiteralInline literal:text.Append(literal.Content.ToString());break;
                    case CodeInline code:warnings.Add("InlineFormattingFlattened");text.Append(code.Content);break;
                    case HtmlEntityInline entity:text.Append(entity.Transcoded.ToString());break;
                    case HtmlInline html:warnings.Add("HtmlShownLiterally");text.Append(html.Tag);break;
                    case LineBreakInline line:text.Append(line.IsHard?'\n':' ');break;
                    case TaskList:break;
                    case LinkInline link:warnings.Add(link.IsImage?"ImagesOmitted":"LinkTargetsOmitted");text.Append(Inline(link,depth+1));break;
                    case AutolinkInline auto:warnings.Add("LinkTargetsOmitted");text.Append(auto.Url);break;
                    case EmphasisInline emphasis:warnings.Add("InlineFormattingFlattened");text.Append(Inline(emphasis,depth+1));break;
                    case ContainerInline nested:warnings.Add("UnsupportedInlineFlattened");text.Append(Inline(nested,depth+1));break;
                    default:warnings.Add("UnsupportedInlineOmitted");break;
                }
                if(text.Length>MaxBodyBytes)throw new DocumentConversionException("CanonicalBodyLimitExceeded");
            }
            return text.ToString();
        }
        private string Plain(ContainerBlock container,int depth)
        {
            Budget(depth);var values=new List<string>();foreach(var block in container)
            {
                Budget(depth);if(block is LeafBlock leaf)values.Add(leaf.Inline is not null?Inline(leaf.Inline,depth+1):leaf.Lines.ToString());
                else if(block is ContainerBlock nested){warnings.Add("NestedStructureFlattened");values.Add(Plain(nested,depth+1));}
            }
            return string.Join('\n',values);
        }
        private void Visit(ContainerBlock container,int depth)
        {
            foreach(var block in container)
            {
                Budget(depth);switch(block)
                {
                    case ParagraphBlock paragraph:Add("paragraph",("text",Inline(paragraph.Inline,depth+1)));break;
                    case HeadingBlock heading:if(heading.Level>3)warnings.Add("HeadingDepthFlattened");Add("heading",("text",Inline(heading.Inline,depth+1)),("level",Math.Min(3,heading.Level)));break;
                    case CodeBlock code:
                        var fields=new List<(string,object)>{("text",code.Lines.ToString())};
                        if(code is FencedCodeBlock fenced&&fenced.Info is {} info)
                        {
                            var language=info.ToString();if(!string.IsNullOrEmpty(language))
                            {if(System.Text.RegularExpressions.Regex.IsMatch(language,@"\A[A-Za-z0-9_+#.\-]{1,64}\z"))fields.Add(("language",language));else warnings.Add("CodeLanguageOmitted");}
                        }
                        Add("code",fields.ToArray());break;
                    case ThematicBreakBlock:Add("divider");break;
                    case QuoteBlock quote:if(quote.Count!=1||quote[0] is not ParagraphBlock)warnings.Add("QuoteStructureFlattened");Add("quote",("text",Plain(quote,depth+1)));break;
                    case ListBlock list:
                        var items=list.OfType<ListItemBlock>().ToArray();if(items.Length>1000)throw new DocumentConversionException("ListLimitExceeded");
                        var checks=items.Select(item=>item.OfType<ParagraphBlock>().FirstOrDefault()?.Inline?.FirstChild as TaskList).ToArray();
                        if(checks.Length>0&&checks.All(item=>item is not null))Add("checklist",("items",items.Select((item,index)=>new{text=Plain(item,depth+1),@checked=checks[index]!.Checked}).ToArray()));
                        else
                        {
                            if(checks.Any(item=>item is not null))warnings.Add("MixedChecklistFlattened");
                            if(list.IsOrdered&&list.OrderedStart!="1")warnings.Add("OrderedListStartNormalized");
                            Add("list",("ordered",list.IsOrdered),("items",items.Select((item,index)=>(checks[index] is {} check?(check.Checked?"☑ ":"☐ "):"")+Plain(item,depth+1)).ToArray()));
                        }break;
                    case Table table:
                        warnings.Add("TableHeaderFormattingFlattened");warnings.Add("TableLayoutNormalized");var rows=table.OfType<TableRow>().Select(row=>row.OfType<TableCell>().Select(cell=>Plain(cell,depth+1)).ToArray()).ToArray();
                        if(rows.Length is <1 or >100||rows.Any(row=>row.Length is <1 or >20))throw new DocumentConversionException("SimpleTableLimitExceeded");
                        var width=rows.Max(row=>row.Length);if(rows.Any(row=>row.Length!=width))warnings.Add("TableColumnsNormalized");
                        Add("table",("rows",rows.Select(row=>row.Concat(Enumerable.Repeat("",width-row.Length)).ToArray()).ToArray()));break;
                    case LeafBlock leaf:warnings.Add("UnsupportedBlockShownLiterally");Add("paragraph",("text",leaf.Lines.ToString()));break;
                    case ContainerBlock nested:warnings.Add("UnsupportedContainerFlattened");Visit(nested,depth+1);break;
                    default:warnings.Add("UnsupportedBlockOmitted");break;
                }
            }
        }
        internal string Read()
        {
            Visit(Markdown.Parse(markdown,ImportPipeline),0);var result=JsonSerializer.Serialize(new{schemaVersion=1,blocks},new JsonSerializerOptions{Encoder=JavaScriptEncoder.Create(UnicodeRanges.All)});
            if(!DocumentBlockSchema.IsValid(result))throw new DocumentConversionException("UnsupportedBlockSchema");return result;
        }
    }
}

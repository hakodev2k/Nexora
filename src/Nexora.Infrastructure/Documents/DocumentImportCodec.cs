using System.IO.Compression;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Nexora.Infrastructure.Documents;

internal sealed record DocumentImportConversion(string Body,string[] Warnings,string SourceDigest);

/// <summary>Bounded local file conversion. No filesystem extraction or relationship resolution.</summary>
internal static partial class DocumentImportCodec
{
    internal const int MaxBodyBytes=1048576;
    internal const int MaxDocxBytes=25*1048576;
    internal static DocumentImportConversion Convert(byte[] bytes,string format,string mode)
    {
        if(mode is not("Markdown" or "Block"))throw new DocumentConversionException("UnsupportedEditorMode");
        var warnings=new HashSet<string>(StringComparer.Ordinal);
        try
        {
            string markdown;
            if(format=="MD"&&bytes.Length<=MaxBodyBytes)markdown=new UTF8Encoding(false,true).GetString(bytes);
            else if(format=="DOCX"&&bytes.Length<=MaxDocxBytes){warnings.Add("WordLayoutStylesNormalized");markdown=new WordReader(bytes,warnings).Read();}
            else throw new DocumentConversionException("SourceLimitOrFormatUnsupported");
            if(mode=="Markdown")DocumentExportCodec.InspectMarkdown(markdown,warnings);
            var body=mode=="Markdown"?markdown:ToBlock(markdown,warnings);
            if(body.Contains('\0')||new UTF8Encoding(false,true).GetByteCount(body)>MaxBodyBytes)throw new DocumentConversionException("CanonicalBodyLimitExceeded");
            return new(body,warnings.Order(StringComparer.Ordinal).ToArray(),System.Convert.ToHexString(SHA256.HashData(bytes)));
        }
        catch(Exception error)when(error is InvalidDataException or IOException or XmlException or DecoderFallbackException or ArgumentException or FormatException or NotSupportedException)
        {throw new DocumentConversionException("InvalidDocument");}
    }
    private static string Escape(string value)=>Regex.Replace(value,@"([\\`*_{}\[\]()#+\-.!|>~])",@"\$1");
    private static void PreflightZip(byte[] bytes)
    {
        int U16(int offset)=>BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset,2));
        uint U32(int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset,4));
        if(bytes.Length<22)throw new DocumentConversionException("InvalidZipLayout");var end=-1;
        for(var i=bytes.Length-4;i>=Math.Max(0,bytes.Length-65557);i--)if(U32(i)==0x06054B50){end=i;break;}
        if(end<0||end+22>bytes.Length||end+22+U16(end+20)!=bytes.Length||U16(end+4)!=0||U16(end+6)!=0||U16(end+8)!=U16(end+10))throw new DocumentConversionException("InvalidZipLayout");
        var expected=U16(end+10);var size=U32(end+12);var offset=U32(end+16);
        if(expected is <1 or >1000||size>MaxDocxBytes||(ulong)offset+size!=(ulong)end||(end>=20&&U32(end-20)==0x07064B50))throw new DocumentConversionException("ZipLimitOrLayoutUnsupported");
        var position=(int)offset;var count=0;long expanded=0;
        while(position<end)
        {
            if(++count>1000||position+46>end||U32(position)!=0x02014B50||U16(position+34)!=0)throw new DocumentConversionException("InvalidZipLayout");
            if((U16(position+8)&1)!=0)throw new DocumentConversionException("EncryptedZipUnsupported");
            if(U16(position+10)is not(0 or 8))throw new DocumentConversionException("ZipCompressionUnsupported");
            var name=U16(position+28);var extra=U16(position+30);var comment=U16(position+32);var next=(long)position+46+name+extra+comment;
            var compressed=U32(position+20);var length=U32(position+24);expanded+=length;
            if(name is <1 or >2000||next>end||length>MaxDocxBytes||compressed>bytes.Length||expanded>MaxDocxBytes||(length>0&&(compressed==0||length/(double)compressed>100))||U32(position+42)>=offset)throw new DocumentConversionException("ZipLimitExceeded");
            var cursor=position+46+name;var extraEnd=cursor+extra;
            while(cursor<extraEnd){if(cursor+4>extraEnd)throw new DocumentConversionException("InvalidZipLayout");var tag=U16(cursor);var amount=U16(cursor+2);if(tag==1)throw new DocumentConversionException("Zip64Unsupported");cursor+=4+amount;if(cursor>extraEnd)throw new DocumentConversionException("InvalidZipLayout");}
            position=(int)next;
        }
        if(count!=expected||position!=end)throw new DocumentConversionException("InvalidZipLayout");
    }

    private sealed class WordReader(byte[] bytes,HashSet<string> warnings)
    {
        private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private readonly Dictionary<string,ZipArchiveEntry> entries=new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,int> headings=new(StringComparer.Ordinal);
        private readonly Dictionary<string,(bool Ordered,int Start)> numbering=new(StringComparer.Ordinal);
        private readonly Dictionary<string,int> counts=new(StringComparer.Ordinal);
        private int xmlNodes;
        private long xmlBytes;
        private int semanticNodes;
        private void Budget(int depth){if(++semanticNodes>10000||depth>64)throw new DocumentConversionException("ConversionLimitExceeded");}
        private XDocument? Xml(string path,bool required=false)
        {
            if(!entries.TryGetValue(path,out var entry)){if(required)throw new DocumentConversionException("InvalidDocument");return null;}
            if(entry.Length>4*1048576)throw new DocumentConversionException("XmlLimitExceeded");
            using var input=entry.Open();using var output=new MemoryStream();var buffer=new byte[8192];int read;
            while((read=input.Read(buffer,0,buffer.Length))>0){xmlBytes+=read;if(output.Length+read>4*1048576||xmlBytes>MaxDocxBytes)throw new DocumentConversionException("XmlLimitExceeded");output.Write(buffer,0,read);}
            if(output.Length!=entry.Length||(output.Length>0&&(entry.CompressedLength<=0||output.Length/(double)entry.CompressedLength>100)))throw new DocumentConversionException("ZipLengthOrRatioInvalid");
            var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=4*1048576,MaxCharactersFromEntities=0};
            output.Position=0;using(var reader=XmlReader.Create(output,settings)){while(reader.Read())if(++xmlNodes>100000||reader.Depth>64)throw new DocumentConversionException("XmlLimitExceeded");}
            output.Position=0;using var final=XmlReader.Create(output,settings);return XDocument.Load(final,LoadOptions.None);
        }
        internal string Read()
        {
            PreflightZip(bytes);
            using var source=new MemoryStream(bytes,false);using var zip=new ZipArchive(source,ZipArchiveMode.Read,false);
            if(zip.Entries.Count>1000)throw new DocumentConversionException("ZipLimitExceeded");long total=0;var decodedNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var entry in zip.Entries)
            {
                var name=entry.FullName;var decoded=Uri.UnescapeDataString(name);
                var segments=decoded.Split('/');
                if(string.IsNullOrEmpty(name)||name.Length>500||name.Any(char.IsControl)||decoded.Any(char.IsControl)||name.Contains('\\')||name.Contains(':')||decoded.Contains(':')||name.StartsWith('/')||decoded.Contains('\\')||decoded.StartsWith('/')||decoded.Count(value=>value=='/')!=name.Count(value=>value=='/')||segments.Any(part=>part is "." or "..")||segments.Take(segments.Length-1).Any(string.IsNullOrEmpty)||!decodedNames.Add(decoded)||!entries.TryAdd(name,entry))throw new DocumentConversionException("UnsafeZipEntry");
                total+=entry.Length;if(entry.Length<0||entry.CompressedLength<0||entry.Length>MaxDocxBytes||total>MaxDocxBytes||(entry.Length>0&&(entry.CompressedLength==0||entry.Length/(double)entry.CompressedLength>100)))throw new DocumentConversionException("ZipLimitExceeded");
                if(decoded.Contains("vbaProject",StringComparison.OrdinalIgnoreCase))throw new DocumentConversionException("MacrosUnsupported");
            }
            var content=Xml("[Content_Types].xml",true)!;
            if(content.Root?.Name!=XName.Get("Types","http://schemas.openxmlformats.org/package/2006/content-types"))throw new DocumentConversionException("InvalidDocument");
            if(content.Descendants().Attributes("ContentType").Any(attribute=>attribute.Value.Contains("macroEnabled",StringComparison.OrdinalIgnoreCase)||attribute.Value.Contains("vbaProject",StringComparison.OrdinalIgnoreCase)))throw new DocumentConversionException("MacrosUnsupported");
            foreach(var name in entries.Keys.Where(name=>name.EndsWith(".rels",StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                var relations=Xml(name)!;
                foreach(var relation in relations.Descendants())
                {
                    if(relation.Attribute("TargetMode")?.Value.Equals("External",StringComparison.OrdinalIgnoreCase)==true)warnings.Add("ExternalRelationshipsOmitted");
                    if(relation.Attribute("Type")?.Value.Contains("vbaProject",StringComparison.OrdinalIgnoreCase)==true)throw new DocumentConversionException("MacrosUnsupported");
                }
            }
            var document=Xml("word/document.xml",true)!;
            if(document.Root?.Name!=W+"document"||document.Root.Element(W+"body") is not {} body)throw new DocumentConversionException("InvalidDocument");
            if(document.Descendants().Any(element=>element.Name.Namespace==W&&element.Name.LocalName is "ins" or "del" or "moveFrom" or "moveTo"))throw new DocumentConversionException("TrackedChangesUnsupported");
            if(document.Descendants().Any(element=>element.Name==W+"fldSimple"||element.Name==W+"fldChar"||element.Name==W+"instrText"))throw new DocumentConversionException("WordFieldsUnsupported");
            LoadStyles();LoadNumbering();var result=new StringBuilder();
            foreach(var element in body.Elements())
            {
                Budget(0);
                if(element.Name==W+"p")result.Append(Paragraph(element)).Append("\n\n");
                else if(element.Name==W+"tbl")result.Append(Table(element)).Append("\n\n");
                else if(element.Name==W+"sectPr"){if(element.HasElements)warnings.Add("PageLayoutOmitted");}
                else {warnings.Add("UnsupportedDocumentStructureFlattened");foreach(var p in element.Descendants(W+"p"))result.Append(Paragraph(p)).Append("\n\n");}
                if(result.Length>MaxBodyBytes)throw new DocumentConversionException("CanonicalBodyLimitExceeded");
            }
            return result.ToString();
        }
        private void LoadStyles()
        {
            var styles=Xml("word/styles.xml");if(styles is null)return;
            foreach(var style in styles.Descendants(W+"style"))
            {
                var id=style.Attribute(W+"styleId")?.Value;var outline=style.Element(W+"pPr")?.Element(W+"outlineLvl")?.Attribute(W+"val")?.Value;
                if(id is not null&&int.TryParse(outline,out var level)&&level is >=0 and <=8)headings[id]=level+1;
            }
        }
        private void LoadNumbering()
        {
            var definitions=Xml("word/numbering.xml");if(definitions is null)return;
            foreach(var num in definitions.Descendants(W+"num"))
            {
                var id=num.Attribute(W+"numId")?.Value;var abstractId=num.Element(W+"abstractNumId")?.Attribute(W+"val")?.Value;if(id is null||abstractId is null)continue;
                var definition=definitions.Descendants(W+"abstractNum").FirstOrDefault(value=>value.Attribute(W+"abstractNumId")?.Value==abstractId);
                foreach(var level in definition?.Elements(W+"lvl")??[])
                {
                    var index=level.Attribute(W+"ilvl")?.Value??"0";var kind=level.Element(W+"numFmt")?.Attribute(W+"val")?.Value;
                    var text=num.Elements(W+"lvlOverride").FirstOrDefault(value=>value.Attribute(W+"ilvl")?.Value==index)?.Element(W+"startOverride")?.Attribute(W+"val")?.Value??level.Element(W+"start")?.Attribute(W+"val")?.Value;
                    var start=int.TryParse(text,out var parsed)&&parsed is >=0 and <=999999999?parsed:1;
                    if(kind is not("decimal" or "bullet"))warnings.Add("ListNumberStyleNormalized");
                    numbering[id+":"+index]=(kind!="bullet",start);
                }
            }
        }
        private string Paragraph(XElement paragraph)
        {
            Budget(1);var properties=paragraph.Element(W+"pPr");var style=properties?.Element(W+"pStyle")?.Attribute(W+"val")?.Value;var heading=0;
            if(style is not null&&!headings.TryGetValue(style,out heading)&&style.StartsWith("Heading",StringComparison.OrdinalIgnoreCase))int.TryParse(style.AsSpan(7),out heading);
            if(heading<0)heading=0;if(heading>6){heading=6;warnings.Add("HeadingDepthFlattened");}
            if(style is not(null or "Normal" or "DefaultParagraphFont")&&heading==0)warnings.Add("FormattingStylesFlattened");
            var text=string.Concat(paragraph.Elements().Where(element=>element.Name!=W+"pPr").Select(element=>Rich(element,2)));
            var num=properties?.Element(W+"numPr");if(num is not null)
            {
                var id=num.Element(W+"numId")?.Attribute(W+"val")?.Value??"";var level=num.Element(W+"ilvl")?.Attribute(W+"val")?.Value??"0";var key=id+":"+level;
                if(!numbering.TryGetValue(key,out var value)){warnings.Add("ListStructureFlattened");text="- "+text;}
                else{var count=counts.TryGetValue(key,out var old)?old+1:value.Start;counts[key]=count;var prefix=value.Ordered?$"{count}. ":"- ";if(level!="0")warnings.Add("NestedListStructureFlattened");text=prefix+text;}
                if(heading>0)warnings.Add("NumberedHeadingFlattened");
            }
            if(properties?.Element(W+"sectPr") is not null)warnings.Add("PageLayoutOmitted");
            return (heading>0?new string('#',heading)+" ":"")+text;
        }
        private string Rich(XElement element,int depth)
        {
            Budget(depth);var name=element.Name.LocalName;
            if(element.Name.NamespaceName=="http://schemas.openxmlformats.org/officeDocument/2006/math"){warnings.Add("EquationsOmitted");return "";}
            if(element.Name==W+"t")return Escape(element.Value).Replace("\r\n","\n",StringComparison.Ordinal).Replace("\n","  \n",StringComparison.Ordinal);
            if(element.Name==W+"rPr")return "";
            if(name is "drawing" or "pict"){warnings.Add("ImagesOmitted");return "";}
            if(name=="object"){warnings.Add("EmbeddedObjectsOmitted");return "";}
            if(name.Contains("comment",StringComparison.OrdinalIgnoreCase)){warnings.Add("CommentsOmitted");return "";}
            if(name is "footnoteReference" or "endnoteReference"){warnings.Add("FootnotesOmitted");return "";}
            if(element.Name==W+"br"||element.Name==W+"cr"){if(element.Attribute(W+"type")?.Value=="page")warnings.Add("PageLayoutOmitted");return "  \n";}
            if(element.Name==W+"tab"){warnings.Add("TabsExpanded");return "    ";}
            if(name is "bookmarkStart" or "bookmarkEnd" or "proofErr" or "lastRenderedPageBreak")return "";
            if(name=="hyperlink")warnings.Add("ExternalRelationshipsOmitted");
            var text=string.Concat(element.Elements().Select(child=>Rich(child,depth+1)));
            if(element.Name==W+"r")
            {
                var properties=element.Element(W+"rPr");bool On(string key)=>properties?.Element(W+key) is {} setting&&setting.Attribute(W+"val")?.Value is not("0" or "false" or "off");
                if(properties?.Element(W+"rStyle")is not null)warnings.Add("FormattingStylesFlattened");
                string Wrap(string value,string delimiter){var core=value.Trim();if(core.Length==0)return value;var start=value.IndexOf(core,StringComparison.Ordinal);if(core.Length!=value.Length)warnings.Add("WhitespaceFormattingNormalized");return value[..start]+delimiter+core+delimiter+value[(start+core.Length)..];}
                if(text.Length>0){if(On("b"))text=Wrap(text,"**");if(On("i"))text=Wrap(text,"*");if(On("strike"))text=Wrap(text,"~~");}
            }
            else if(name is not("hyperlink" or "smartTag" or "sdt" or "sdtContent"))warnings.Add("UnsupportedInlineStructureFlattened");
            return text;
        }
        private string Table(XElement table)
        {
            warnings.Add("TableFirstRowBecomesHeader");var rows=new List<List<string>>();
            foreach(var row in table.Elements(W+"tr"))
            {
                var cells=new List<string>();foreach(var cell in row.Elements(W+"tc"))
                {
                    Budget(2);if(cell.Element(W+"tcPr")?.Elements().Any(value=>value.Name==W+"gridSpan"||value.Name==W+"vMerge")==true)warnings.Add("MergedCellsFlattened");
                    if(cell.Elements(W+"tbl").Any())warnings.Add("NestedTablesOmitted");var paragraphs=cell.Elements(W+"p").ToArray();if(paragraphs.Length>1)warnings.Add("TableCellBlocksFlattened");
                    var text=string.Join(" / ",paragraphs.Select(Paragraph));if(text.Contains('\n')||text.Contains('\r'))warnings.Add("TableCellLineBreaksFlattened");cells.Add(text.Replace('\n',' ').Replace('\r',' '));
                }
                if(cells.Count is <1 or >20||rows.Count>=100)throw new DocumentConversionException("SimpleTableLimitExceeded");rows.Add(cells);
            }
            if(rows.Count==0)return "";var width=rows.Max(row=>row.Count);if(rows.Any(row=>row.Count!=width))warnings.Add("TableColumnsNormalized");var output=new StringBuilder();
            for(var i=0;i<rows.Count;i++){output.Append("| ").Append(string.Join(" | ",rows[i].Concat(Enumerable.Repeat("",width-rows[i].Count)))).Append(" |\n");if(i==0)output.Append("| ").Append(string.Join(" | ",Enumerable.Repeat("---",width))).Append(" |\n");}return output.ToString();
        }
    }
}

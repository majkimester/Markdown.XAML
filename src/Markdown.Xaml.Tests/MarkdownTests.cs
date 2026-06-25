using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;

using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;

namespace Markdown.Xaml.Tests
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class MarkdownTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_01_Headings_generatesExpectedResult()
        {
            var text = LoadText("01_Headings.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        public void Transform_02_Paragraphs_generatesExpectedResult()
        {
            var text = LoadText("02_Paragraphs.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_03_TextStyles_generatesExpectedResult()
        {
            var text = LoadText("03_TextStyles.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_04_Blockquotes_generatesExpectedResult()
        {
            var text = LoadText("04_Blockquotes.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_05_Lists_generatesExpectedResult()
        {
            var text = LoadText("05_Lists.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        public void Transform_06_Tables_generatesExpectedResult() {
            var text = LoadText("06_Tables.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_07_HorizontalRules_generatesExpectedResult()
        {
            var text = LoadText("07_HorizontalRules.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_08_LinksInline_generatesExpectedResult()
        {
            var text = LoadText("08_LinksInline.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_09_Images_generatesExpectedResult()
        {
            var text = LoadText("09_Images.md");
            var markdown = GetMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(AsXaml(result));
        }

        private string LoadText(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream("Markdown.Xaml.Tests." + name))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private string AsXaml(object instance)
        {
            if (instance is FlowDocument doc)
            {
                // 1. Handle FlowDocument style
                if (doc.Style != null && doc.Style.Resources["Id"] is string docStyleId)
                {
                    doc.Tag = $"StaticResource {docStyleId}";
                    doc.Style = null;
                }

                // 2. Recursively handle all nested children at any level
                foreach (var block in doc.Blocks)
                {
                    ProcessFrameworkContentElement(block);
                }
            }

            string xmlOutput;
            using (var writer = new StringWriter())
            {
                var settings = new XmlWriterSettings { Indent = true };
                using (var xmlWriter = XmlWriter.Create(writer, settings))
                {
                    XamlWriter.Save(instance, xmlWriter);
                }

                writer.WriteLine();
                xmlOutput = writer.ToString();
            }

            // 3. Remove style Null first
            xmlOutput = System.Text.RegularExpressions.Regex.Replace(
                xmlOutput,
                @"Style=""\{x:Null\}""\s*",
                ""
            );

            // Replace Tab with Style to be able to ckeck
            xmlOutput = System.Text.RegularExpressions.Regex.Replace(
                xmlOutput,
                @"Tag=""StaticResource (.*?)""",
                @"Style=""{StaticResource $1}"""
            );

            return xmlOutput;
        }

        private void ProcessFrameworkContentElement(object element)
        {
            if (element == null) return;

            // Convert mock styles to Tags for FrameworkContentElement types (Blocks, Inlines, RowGroups, Rows)
            if (element is FrameworkContentElement fce && fce.Style != null)
            {
                if (fce.Style.Resources["Id"] is string styleId)
                {
                    fce.Tag = $"StaticResource {styleId}";
                    fce.Style = null;
                }
            }
            // Fallback convert for standard UI elements that might be embedded (like Image or Separator)
            else if (element is FrameworkElement fe && fe.Style != null)
            {
                if (fe.Style.Resources["Id"] is string styleId)
                {
                    fe.Tag = $"StaticResource {styleId}";
                    fe.Style = null;
                }
            }

            // --- RECURSIVE TREE TRAVERSAL ---

            // Handle standard layout Panels (StackPanel, Grid, WrapPanel, etc.)
            if (element is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    ProcessFrameworkContentElement(child);
                }
            }
            // Handle Paragraphs and structural blocks that contain inline text elements (Hyperlink, Run, etc.)
            if (element is Paragraph p)
            {
                foreach (var inline in p.Inlines)
                {
                    ProcessFrameworkContentElement(inline);
                }
            }
            // Handle Span or Hyperlinks which can contain further nested inline elements
            else if (element is Span span)
            {
                foreach (var inline in span.Inlines)
                {
                    ProcessFrameworkContentElement(inline);
                }
            }
            // Handle Tables (Traverse through RowGroups -> Rows -> Cells -> Blocks)
            else if (element is Table table)
            {
                foreach (var rowGroup in table.RowGroups)
                {
                    ProcessFrameworkContentElement(rowGroup);
                    foreach (var row in rowGroup.Rows)
                    {
                        ProcessFrameworkContentElement(row);
                        foreach (var cell in row.Cells)
                        {
                            foreach (var cellBlock in cell.Blocks)
                            {
                                ProcessFrameworkContentElement(cellBlock);
                            }
                        }
                    }
                }
            }
            // Handle List structures (Traverse through ListItems -> Blocks)
            else if (element is System.Windows.Documents.List list)
            {
                foreach (var listItem in list.ListItems)
                {
                    foreach (var itemBlock in listItem.Blocks)
                    {
                        ProcessFrameworkContentElement(itemBlock);
                    }
                }
            }
            // Handle RichTextBox or Section blocks that contain child blocks
            else if (element is Section section)
            {
                foreach (var subBlock in section.Blocks)
                {
                    ProcessFrameworkContentElement(subBlock);
                }
            }
            // Handle UIElementPlaceholders (This is where embedded Images or Separators live inside text trees)
            else if (element is InlineUIContainer uiContainer && uiContainer.Child != null)
            {
                ProcessFrameworkContentElement(uiContainer.Child);
            }
            else if (element is BlockUIContainer blockUiContainer && blockUiContainer.Child != null)
            {
                ProcessFrameworkContentElement(blockUiContainer.Child);
            }
        }

        private Markdown GetMarkdown()
        {
            // Create light, empty dummy styles containing only their TargetType 
            // to act as trace identifiers (Id) during evaluation.
            return new Markdown
            {
                // Document and text block styles
                DocumentStyle = new Style(typeof(FlowDocument)) { Resources = { { "Id", "DocumentStyle" } } },
                BlockQuoteStyle = new Style(typeof(RichTextBox)) { Resources = { { "Id", "BlockQuoteStyle" } } }, // Inherits RichTextBox in the source
                CodeBlockStyle = new Style(typeof(Paragraph)) { Resources = { { "Id", "CodeBlockStyle" } } },
                NormalParagraphStyle = new Style(typeof(Paragraph)) { Resources = { { "Id", "NormalParagraphStyle" } } },
                NoteStyle = new Style(typeof(Paragraph)) { Resources = { { "Id", "NoteStyle" } } },

                // Heading styles
                Heading1Style = new Style(typeof(Paragraph)) { Resources = { { "Id", "H1Style" } } },
                Heading2Style = new Style(typeof(Paragraph)) { Resources = { { "Id", "H2Style" } } },
                Heading3Style = new Style(typeof(Paragraph)) { Resources = { { "Id", "H3Style" } } },
                Heading4Style = new Style(typeof(Paragraph)) { Resources = { { "Id", "H4Style" } } },
                Heading5Style = new Style(typeof(Paragraph)) { Resources = { { "Id", "H5Style" } } },
                Heading6Style = new Style(typeof(Paragraph)) { Resources = { { "Id", "H6Style" } } },

                // Inline and embedded element styles
                CodeSpanStyle = new Style(typeof(Run)) { Resources = { { "Id", "CodeSpanStyle" } } },
                LinkStyle = new Style(typeof(Hyperlink)) { Resources = { { "Id", "LinkStyle" } } },
                ImageStyle = new Style(typeof(Image)) { Resources = { { "Id", "ImageStyle" } } },
                ImageFailedStyle = new Style(typeof(Image)) { Resources = { { "Id", "ImageFailedStyle" } } },

                // Separators and layout formatting styles
                SeparatorStyle = new Style(typeof(Separator)) { Resources = { { "Id", "SeparatorStyle" } } },
                TableStyle = new Style(typeof(Table)) { Resources = { { "Id", "TableStyle" } } },
                TableHeaderStyle = new Style(typeof(TableRowGroup)) { Resources = { { "Id", "TableHeaderStyle" } } },
                TableBodyStyle = new Style(typeof(TableRowGroup)) { Resources = { { "Id", "TableBodyStyle" } } },
                TableRowAltStyle = new Style(typeof(TableRow)) { Resources = { { "Id", "TableRowAltStyle" } } },

                // Environment variables
                AssetPathRoot = Environment.CurrentDirectory
            };
        }
    }
}

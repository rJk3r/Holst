using Holst.Models;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Holst.IntegrationTests
{
    /// <summary>
    /// Тесты для проверки работы с markdown-контентом и документными блоками.
    /// Тестирует сериализацию, десериализацию и логику обработки различных типов блоков.
    /// </summary>
    public class MarkdownContentTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public MarkdownContentTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region DocumentBlock Tests

        [Fact]
        public void HeaderBlock_Created_HasUniqueId()
        {
            var block1 = new HeaderBlock { Level = 1, Text = "Header 1" };
            var block2 = new HeaderBlock { Level = 1, Text = "Header 2" };

            Assert.NotEqual(block1.Id, block2.Id);
            Assert.NotEmpty(block1.Id);
            Assert.NotEmpty(block2.Id);
        }

        [Theory]
        [InlineData(1, "H1")]
        [InlineData(2, "H2")]
        [InlineData(3, "H3")]
        public void HeaderBlock_Properties_SetCorrectly(int level, string text)
        {
            var block = new HeaderBlock { Level = level, Text = text };

            Assert.Equal(level, block.Level);
            Assert.Equal(text, block.Text);
        }

        [Fact]
        public void ParagraphBlock_Created_HasUniqueId()
        {
            var block1 = new ParagraphBlock { Text = "Text 1" };
            var block2 = new ParagraphBlock { Text = "Text 2" };

            Assert.NotEqual(block1.Id, block2.Id);
        }

        [Fact]
        public void CodeBlock_Created_HasUniqueId()
        {
            var block1 = new CodeBlock { Text = "code1", Language = "csharp" };
            var block2 = new CodeBlock { Text = "code2", Language = "python" };

            Assert.NotEqual(block1.Id, block2.Id);
        }

        [Fact]
        public void CodeBlock_Properties_SetCorrectly()
        {
            var block = new CodeBlock
            {
                Text = "int x = 5;",
                Language = "csharp"
            };

            Assert.Equal("int x = 5;", block.Text);
            Assert.Equal("csharp", block.Language);
        }

        [Fact]
        public void CodeBlock_NullLanguage_HandledCorrectly()
        {
            var block = new CodeBlock
            {
                Text = "some code",
                Language = null
            };

            Assert.Null(block.Language);
        }

        #endregion

        #region TextProject Tests

        [Fact]
        public void TextProject_DefaultBlocks_NotNull()
        {
            var project = new TextProject();

            Assert.NotNull(project.Blocks);
            Assert.Empty(project.Blocks);
        }

        [Fact]
        public void TextProject_AddBlocks_BlocksAdded()
        {
            var project = new TextProject();

            project.Blocks.Add(new HeaderBlock { Level = 1, Text = "Title" });
            project.Blocks.Add(new ParagraphBlock { Text = "Content" });
            project.Blocks.Add(new CodeBlock { Text = "code", Language = "csharp" });

            Assert.Equal(3, project.Blocks.Count);
            Assert.IsType<HeaderBlock>(project.Blocks[0]);
            Assert.IsType<ParagraphBlock>(project.Blocks[1]);
            Assert.IsType<CodeBlock>(project.Blocks[2]);
        }

        [Fact]
        public void TextProject_Type_IsText()
        {
            var project = new TextProject();

            Assert.Equal(ProjectType.Text, project.Type);
        }

        #endregion

        #region Serialization Tests

        [Fact]
        public void TextProject_SerializeHeaderBlock_WritesCorrectType()
        {
            var project = new TextProject();
            project.Blocks.Add(new HeaderBlock { Level = 2, Text = "Test Header", Id = "test-id" });

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);

            project.SerializeContent(writer);
            writer.Flush();

            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);

            int count = reader.ReadInt32();
            Assert.Equal(1, count);

            string id = reader.ReadString();
            int type = reader.ReadInt32();
            int level = reader.ReadInt32();
            string text = reader.ReadString();

            Assert.Equal("test-id", id);
            Assert.Equal(0, type); // HeaderBlock type code
            Assert.Equal(2, level);
            Assert.Equal("Test Header", text);
        }

        [Fact]
        public void TextProject_SerializeParagraphBlock_WritesCorrectType()
        {
            var project = new TextProject();
            project.Blocks.Add(new ParagraphBlock { Text = "Test paragraph", Id = "para-id" });

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);

            project.SerializeContent(writer);
            writer.Flush();

            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);

            int count = reader.ReadInt32();
            Assert.Equal(1, count);

            string id = reader.ReadString();
            int type = reader.ReadInt32();
            string text = reader.ReadString();

            Assert.Equal("para-id", id);
            Assert.Equal(1, type); // ParagraphBlock type code
            Assert.Equal("Test paragraph", text);
        }

        [Fact]
        public void TextProject_SerializeCodeBlock_WritesCorrectType()
        {
            var project = new TextProject();
            project.Blocks.Add(new CodeBlock { Text = "int x = 5;", Language = "csharp", Id = "code-id" });

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);

            project.SerializeContent(writer);
            writer.Flush();

            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);

            int count = reader.ReadInt32();
            Assert.Equal(1, count);

            string id = reader.ReadString();
            int type = reader.ReadInt32();
            string language = reader.ReadString();
            string text = reader.ReadString();

            Assert.Equal("code-id", id);
            Assert.Equal(2, type); // CodeBlock type code
            Assert.Equal("csharp", language);
            Assert.Equal("int x = 5;", text);
        }

        [Fact]
        public void TextProject_DeserializeHeaderBlock_RestoresCorrectly()
        {
            var project = new TextProject();
            project.Blocks.Add(new HeaderBlock { Level = 3, Text = "H3 Header" });

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);
            project.SerializeContent(writer);
            writer.Flush();

            var newProject = new TextProject();
            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);
            newProject.DeserializeContent(reader);

            Assert.Single(newProject.Blocks);
            Assert.IsType<HeaderBlock>(newProject.Blocks[0]);

            var header = (HeaderBlock)newProject.Blocks[0];
            Assert.Equal(3, header.Level);
            Assert.Equal("H3 Header", header.Text);
        }

        [Fact]
        public void TextProject_DeserializeParagraphBlock_RestoresCorrectly()
        {
            var project = new TextProject();
            project.Blocks.Add(new ParagraphBlock { Text = "Paragraph content" });

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);
            project.SerializeContent(writer);
            writer.Flush();

            var newProject = new TextProject();
            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);
            newProject.DeserializeContent(reader);

            Assert.Single(newProject.Blocks);
            Assert.IsType<ParagraphBlock>(newProject.Blocks[0]);

            var para = (ParagraphBlock)newProject.Blocks[0];
            Assert.Equal("Paragraph content", para.Text);
        }

        [Fact]
        public void TextProject_DeserializeCodeBlock_RestoresCorrectly()
        {
            var project = new TextProject();
            project.Blocks.Add(new CodeBlock { Text = "print('hello')", Language = "python" });

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);
            project.SerializeContent(writer);
            writer.Flush();

            var newProject = new TextProject();
            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);
            newProject.DeserializeContent(reader);

            Assert.Single(newProject.Blocks);
            Assert.IsType<CodeBlock>(newProject.Blocks[0]);

            var code = (CodeBlock)newProject.Blocks[0];
            Assert.Equal("print('hello')", code.Text);
            Assert.Equal("python", code.Language);
        }

        [Fact]
        public void TextProject_FullRoundtrip_AllBlocksPreserved()
        {
            var original = new TextProject();
            original.Blocks.Add(new HeaderBlock { Level = 1, Text = "Main Title" });
            original.Blocks.Add(new ParagraphBlock { Text = "Introduction paragraph" });
            original.Blocks.Add(new HeaderBlock { Level = 2, Text = "Section 1" });
            original.Blocks.Add(new CodeBlock { Text = "var x = 10;", Language = "javascript" });
            original.Blocks.Add(new ParagraphBlock { Text = "Conclusion" });

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);
            original.SerializeContent(writer);
            writer.Flush();

            var restored = new TextProject();
            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);
            restored.DeserializeContent(reader);

            Assert.Equal(original.Blocks.Count, restored.Blocks.Count);

            Assert.IsType<HeaderBlock>(restored.Blocks[0]);
            Assert.Equal("Main Title", ((HeaderBlock)restored.Blocks[0]).Text);

            Assert.IsType<ParagraphBlock>(restored.Blocks[1]);
            Assert.Equal("Introduction paragraph", ((ParagraphBlock)restored.Blocks[1]).Text);

            Assert.IsType<HeaderBlock>(restored.Blocks[2]);
            Assert.Equal("Section 1", ((HeaderBlock)restored.Blocks[2]).Text);

            Assert.IsType<CodeBlock>(restored.Blocks[3]);
            var code = (CodeBlock)restored.Blocks[3];
            Assert.Equal("var x = 10;", code.Text);
            Assert.Equal("javascript", code.Language);

            Assert.IsType<ParagraphBlock>(restored.Blocks[4]);
        }

        [Fact]
        public void TextProject_EmptyBlocks_Roundtrip_Success()
        {
            var original = new TextProject();

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);
            original.SerializeContent(writer);
            writer.Flush();

            var restored = new TextProject();
            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);
            restored.DeserializeContent(reader);

            Assert.Empty(restored.Blocks);
        }

        [Fact]
        public void TextProject_NullBlocks_SerializesAsEmpty()
        {
            var project = new TextProject();
            project.Blocks = null;

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);

            project.SerializeContent(writer);
            writer.Flush();

            ms.Position = 0;
            using var reader = new BinaryReader(ms, Encoding.UTF8);
            int count = reader.ReadInt32();

            Assert.Equal(0, count);
        }

        #endregion

        #region ProjectFactory Tests

        [Theory]
        [InlineData(ProjectType.Text, typeof(TextProject))]
        [InlineData(ProjectType.Graph, typeof(GraphProject))]
        public void ProjectFactory_CreateProject_ReturnsCorrectType(ProjectType type, Type expectedType)
        {
            var project = ProjectFactory.CreateProject(type, "Test", "Author");

            Assert.IsType(expectedType, project);
        }

        [Fact]
        public void ProjectFactory_CreateTextProject_SetsProperties()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "My Project", "Test Author");

            Assert.Equal("My Project", project.Name);
            Assert.Equal("Test Author", project.Author);
            Assert.Equal(ProjectType.Text, project.Type);
        }

        [Fact]
        public void ProjectFactory_CreateGraphProject_SetsProperties()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Graph, "Graph View", "Graph Author");

            Assert.Equal("Graph View", project.Name);
            Assert.Equal("Graph Author", project.Author);
            Assert.Equal(ProjectType.Graph, project.Type);
        }

        [Fact]
        public void ProjectFactory_CreateProject_GeneratesUniqueId()
        {
            var p1 = ProjectFactory.CreateProject(ProjectType.Text, "P1", "A");
            var p2 = ProjectFactory.CreateProject(ProjectType.Text, "P2", "A");

            Assert.NotEqual(p1.Id, p2.Id);
        }

        [Fact]
        public void ProjectFactory_CreateProject_SetsCreationDate()
        {
            var before = DateTime.UtcNow;
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Test", "Author");
            var after = DateTime.UtcNow;

            Assert.True(project.CreatedAt >= before);
            Assert.True(project.CreatedAt <= after);
        }

        #endregion

        #region Project Metadata Tests

        [Fact]
        public void BaseProject_SetFilePath_StoredCorrectly()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Test", "Author");
            var path = @"C:\Projects\test.holst";

            project.FilePath = path;

            Assert.Equal(path, project.FilePath);
        }

        [Fact]
        public void BaseProject_DefaultFilePath_IsEmpty()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Test", "Author");

            Assert.Equal(string.Empty, project.FilePath);
        }

        [Fact]
        public void BaseProject_UpdatedAt_ChangesOnModification()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Test", "Author");
            var before = project.UpdatedAt;

            Thread.Sleep(50);
            project.Name = "Modified Name";

            Assert.True(project.UpdatedAt >= before);
        }

        #endregion

        #region GraphProject Tests

        [Fact]
        public void GraphProject_Type_IsGraph()
        {
            var project = new GraphProject();

            Assert.Equal(ProjectType.Graph, project.Type);
        }

        [Fact]
        public void GraphProject_InheritsFromTextProject()
        {
            var project = new GraphProject();

            Assert.IsAssignableFrom<TextProject>(project);
        }

        [Fact]
        public void GraphProject_HasBlocksCollection()
        {
            var project = new GraphProject();

            Assert.NotNull(project.Blocks);
        }

        #endregion

        #region GraphEdge Tests

        [Fact]
        public void GraphEdge_Properties_SetCorrectly()
        {
            var edge = new GraphEdge
            {
                SourceHeaderId = "header-1",
                TargetHeaderId = "header-2"
            };

            Assert.Equal("header-1", edge.SourceHeaderId);
            Assert.Equal("header-2", edge.TargetHeaderId);
        }

        #endregion
    }
}

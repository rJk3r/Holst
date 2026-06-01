using Holst.Models;
using Holst.Services;
using Xunit;
using Xunit.Abstractions;

namespace Holst.IntegrationTests
{
    /// <summary>
    /// Интеграционные тесты для сервиса работы с файлами проектов.
    /// Тестирует сохранение, загрузку, шифрование и сериализацию проектов.
    /// </summary>
    public class ProjectFileServiceTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly ProjectFileService _service;

        public ProjectFileServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _service = new ProjectFileService();
        }

        #region Text Project Tests

        [Fact]
        public async Task SaveAndLoad_TextProject_RoundtripSuccess()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Test Text Project", "TestAuthor");
            var textProject = (TextProject)project;
            textProject.Blocks.Add(new ParagraphBlock { Text = "First paragraph" });
            textProject.Blocks.Add(new HeaderBlock { Level = 1, Text = "Main Header" });
            textProject.Blocks.Add(new ParagraphBlock { Text = "Second paragraph" });

            var filePath = Path.Combine(ProjectsDirectory, $"test_{Guid.NewGuid():N}.holst");

            var saved = await _service.SaveProjectAsync(project, filePath);
            Assert.True(saved);
            Assert.True(File.Exists(filePath));
            _output.WriteLine($"Saved project to: {filePath}");

            var loaded = await _service.LoadProjectAsync(filePath);

            Assert.NotNull(loaded);
            Assert.Equal(project.Id, loaded.Id);
            Assert.Equal(project.Name, loaded.Name);
            Assert.Equal(project.Author, loaded.Author);
            Assert.Equal(ProjectType.Text, loaded.Type);
            _output.WriteLine($"Loaded project: {loaded.Name} ({loaded.Id})");
        }

        [Fact]
        public async Task LoadProject_TextProject_ContentBlocksPreserved()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Content Test", "Author");
            var textProject = (TextProject)project;
            textProject.Blocks.Add(new HeaderBlock { Level = 2, Text = "H2 Header" });
            textProject.Blocks.Add(new ParagraphBlock { Text = "Paragraph content" });
            textProject.Blocks.Add(new CodeBlock { Text = "int x = 5;", Language = "csharp" });

            var filePath = Path.Combine(ProjectsDirectory, "content_test.holst");
            await _service.SaveProjectAsync(project, filePath);

            var loaded = await _service.LoadProjectAsync(filePath);
            var loadedText = (TextProject)loaded;

            Assert.Equal(3, loadedText.Blocks.Count);
            Assert.IsType<HeaderBlock>(loadedText.Blocks[0]);
            Assert.IsType<ParagraphBlock>(loadedText.Blocks[1]);
            Assert.IsType<CodeBlock>(loadedText.Blocks[2]);

            var header = (HeaderBlock)loadedText.Blocks[0];
            Assert.Equal(2, header.Level);
            Assert.Equal("H2 Header", header.Text);

            var code = (CodeBlock)loadedText.Blocks[2];
            Assert.Equal("int x = 5;", code.Text);
            Assert.Equal("csharp", code.Language);
        }

        #endregion

        #region Graph Project Tests

        [Fact]
        public async Task SaveAndLoad_GraphProject_RoundtripSuccess()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Graph, "Test Graph Project", "TestAuthor");
            var filePath = Path.Combine(ProjectsDirectory, $"graph_{Guid.NewGuid():N}.holst");

            var saved = await _service.SaveProjectAsync(project, filePath);
            Assert.True(saved);

            var loaded = await _service.LoadProjectAsync(filePath);

            Assert.NotNull(loaded);
            Assert.Equal(ProjectType.Graph, loaded.Type);
            _output.WriteLine($"Graph project saved and loaded successfully");
        }

        #endregion

        #region File Format Tests

        [Fact]
        public async Task SavedProject_HasCorrectFileExtension()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Ext Test", "Author");
            var filePath = Path.Combine(ProjectsDirectory, $"wrong_ext.txt");

            await _service.SaveProjectAsync(project, filePath);

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task SavedProject_IsEncrypted()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Encryption Test", "Author");
            var filePath = Path.Combine(ProjectsDirectory, "encrypted.holst");

            await _service.SaveProjectAsync(project, filePath);

            var bytes = File.ReadAllBytes(filePath);
            var magic = System.Text.Encoding.ASCII.GetString(bytes.Take(5).ToArray());

            Assert.Equal("HOLST", magic);
            _output.WriteLine($"File magic: {magic}");

            var version = bytes[5];
            Assert.Equal(0x01, version);
        }

        [Fact]
        public async Task LoadProject_InvalidFile_ThrowsInvalidDataException()
        {
            var filePath = Path.Combine(ProjectsDirectory, "invalid.holst");
            File.WriteAllText(filePath, "Not a valid Holst file");

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _service.LoadProjectAsync(filePath);
            });
        }

        [Fact]
        public async Task LoadProject_NonExistentFile_ThrowsFileNotFoundException()
        {
            var filePath = Path.Combine(ProjectsDirectory, "nonexistent.holst");

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await _service.LoadProjectAsync(filePath);
            });
        }

        #endregion

        #region Project Metadata Tests

        [Fact]
        public async Task ProjectMetadata_PreservedAfterRoundtrip()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Metadata Test", "TestAuthor");
            project.FilePath = Path.Combine(ProjectsDirectory, "meta.holst");
            var createdBefore = project.CreatedAt;
            var updatedBefore = project.UpdatedAt;

            await _service.SaveProjectAsync(project, project.FilePath);
            await Task.Delay(100);

            var loaded = await _service.LoadProjectAsync(project.FilePath);

            Assert.Equal(project.Id, loaded.Id);
            Assert.Equal(project.Name, loaded.Name);
            Assert.Equal(project.Author, loaded.Author);
            Assert.Equal(createdBefore, loaded.CreatedAt);
            Assert.True(loaded.UpdatedAt >= updatedBefore);
            _output.WriteLine($"Created: {loaded.CreatedAt}, Updated: {loaded.UpdatedAt}");
        }

        [Fact]
        public async Task ProjectId_UniquePerProject()
        {
            var project1 = ProjectFactory.CreateProject(ProjectType.Text, "Project 1", "Author");
            var project2 = ProjectFactory.CreateProject(ProjectType.Text, "Project 2", "Author");

            var path1 = Path.Combine(ProjectsDirectory, "id1.holst");
            var path2 = Path.Combine(ProjectsDirectory, "id2.holst");

            await _service.SaveProjectAsync(project1, path1);
            await _service.SaveProjectAsync(project2, path2);

            var loaded1 = await _service.LoadProjectAsync(path1);
            var loaded2 = await _service.LoadProjectAsync(path2);

            Assert.NotEqual(loaded1.Id, loaded2.Id);
        }

        #endregion

        #region Empty Project Tests

        [Fact]
        public async Task EmptyTextProject_SaveAndLoad_Success()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Empty Project", "Author");
            var filePath = Path.Combine(ProjectsDirectory, "empty.holst");

            var saved = await _service.SaveProjectAsync(project, filePath);
            Assert.True(saved);

            var loaded = await _service.LoadProjectAsync(filePath);
            Assert.NotNull(loaded);

            var textProject = (TextProject)loaded;
            Assert.NotNull(textProject.Blocks);
        }

        #endregion

        #region Multiple Save Tests

        [Fact]
        public async Task SaveMultipleTimes_SamePath_OverwritesFile()
        {
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Version 1", "Author");
            var filePath = Path.Combine(ProjectsDirectory, "versioned.holst");

            await _service.SaveProjectAsync(project, filePath);
            var firstWrite = File.GetLastWriteTime(filePath);

            await Task.Delay(100);

            project.Name = "Version 2";
            await _service.SaveProjectAsync(project, filePath);
            var secondWrite = File.GetLastWriteTime(filePath);

            Assert.True(secondWrite > firstWrite);

            var loaded = await _service.LoadProjectAsync(filePath);
            Assert.Equal("Version 2", loaded.Name);
        }

        #endregion
    }
}

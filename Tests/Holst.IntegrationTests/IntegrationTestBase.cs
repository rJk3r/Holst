using System.IO;
using Xunit;

namespace Holst.IntegrationTests
{
    /// <summary>
    /// Базовый класс для всех интеграционных тестов.
    /// Обеспечивает общую настройку и очистку тестового окружения.
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly string TestDirectory;
        protected readonly string ProjectsDirectory;

        protected IntegrationTestBase()
        {
            TestDirectory = Path.Combine(Path.GetTempPath(), $"HolstTests_{Guid.NewGuid():N}");
            ProjectsDirectory = Path.Combine(TestDirectory, "Projects");
            Directory.CreateDirectory(TestDirectory);
            Directory.CreateDirectory(ProjectsDirectory);
        }

        public virtual void Dispose()
        {
            try
            {
                if (Directory.Exists(TestDirectory))
                {
                    Directory.Delete(TestDirectory, recursive: true);
                }
            }
            catch { }
            GC.SuppressFinalize(this);
        }
    }
}

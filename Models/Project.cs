using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Models
{
    public enum ProjectType { Text, Graph, Canvas, Diagram }

    public abstract class BaseProject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Author { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public abstract ProjectType Type { get; }

        protected internal virtual void SerializeContent(BinaryWriter writer) { }
        protected internal virtual void DeserializeContent(BinaryReader reader) { }
    }

    #region Text project

    public abstract class DocumentBlock
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    public class HeaderBlock : DocumentBlock
    {
        public int Level { get; set; } // # - 1; ## - 2; ### - 3
        public string Text { get; set; } = string.Empty;
    }

    public class ParagraphBlock : DocumentBlock
    {
        public string Text { get; set; } = string.Empty;
    }

    public class CodeBlock : DocumentBlock
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }

    public class TextProject : BaseProject
    {
        public override ProjectType Type => ProjectType.Text;
        public List<DocumentBlock> Blocks { get; set; } = new List<DocumentBlock>();

        protected internal override void SerializeContent(BinaryWriter writer)
        {
            writer.Write(Blocks?.Count ?? 0);
            if (Blocks == null) return;
            foreach (var block in Blocks)
            {
                writer.Write(block.Id ?? Guid.NewGuid().ToString());
                if (block is HeaderBlock h)
                {
                    writer.Write(0);
                    writer.Write(h.Level);
                    writer.Write(h.Text ?? string.Empty);
                }
                else if (block is ParagraphBlock p)
                {
                    writer.Write(1);
                    writer.Write(p.Text ?? string.Empty);
                }
                else if (block is CodeBlock c)
                {
                    writer.Write(2);
                    writer.Write(c.Language ?? string.Empty);
                    writer.Write(c.Text ?? string.Empty);
                }
                else
                {
                    writer.Write(1);
                    writer.Write(block.ToString() ?? string.Empty);
                }
            }
        }

        protected internal override void DeserializeContent(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Blocks = new List<DocumentBlock>(count);
            for (int i = 0; i < count; i++)
            {
                string id = reader.ReadString();
                int type = reader.ReadInt32();
                if (type == 0)
                {
                    Blocks.Add(new HeaderBlock
                    {
                        Id = id,
                        Level = reader.ReadInt32(),
                        Text = reader.ReadString()
                    });
                }
                else if (type == 2)
                {
                    Blocks.Add(new CodeBlock
                    {
                        Id = id,
                        Language = reader.ReadString(),
                        Text = reader.ReadString()
                    });
                }
                else
                {
                    Blocks.Add(new ParagraphBlock
                    {
                        Id = id,
                        Text = reader.ReadString()
                    });
                }
            }
        }
    }

    public class GraphProject : TextProject
    {
        public override ProjectType Type => ProjectType.Graph;
        
        //public List<GraphEdge> Links { get; set; } = new();
    }

    public class GraphEdge
    {
        public string SourceHeaderId { get; set; } = string.Empty;
        public string TargetHeaderId { get; set; } = string.Empty;
    }
    #endregion

    #region HOLST class / Diagram class
    // TODO: public abstract class vector, primitive & other


    #endregion

    #region ProjectFactory

    /// <summary>
    /// Factory Method Pattern: интерфейс для создания проектов различных типов.
    /// Позволяет подменять реализацию для тестирования и расширяемости.
    /// </summary>
    public interface IProjectFactory
    {
        /// <summary>
        /// Создаёт проект указанного типа с заданными именем и автором.
        /// </summary>
        /// <param name="type">Тип проекта (Text, Graph, Canvas, Diagram)</param>
        /// <param name="name">Имя проекта</param>
        /// <param name="author">Автор проекта</param>
        /// <returns>Созданный проект указанного типа</returns>
        /// <exception cref="ArgumentException">Если тип проекта не поддерживается</exception>
        BaseProject CreateProject(ProjectType type, string name, string author);
    }

    /// <summary>
    /// Стандартная реализация Factory Method для создания проектов.
    /// </summary>
    public class ProjectFactory : IProjectFactory
    {
        /// <summary>
        /// Создаёт проект указанного типа.
        /// </summary>
        public BaseProject CreateProject(ProjectType type, string name, string author)
        {
            BaseProject project = type switch
            {
                ProjectType.Text => new TextProject(),
                ProjectType.Graph => new GraphProject(),
                //ProjectType.Canvas => new CanvasProject(),
                //ProjectType.Diagram => new DiagramProject(),
                _ => throw new ArgumentException($"Неизвестный тип проекта: {type}")
            };

            project.Name = name;
            project.Author = author;
            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;

            return project;
        }
    }

    /// <summary>
    /// Статический фасад для обратной совместимости с кодом, использующим ProjectFactory.CreateProject().
    /// </summary>
    public static class ProjectFactoryExtensions
    {
        private static IProjectFactory _factory = new ProjectFactory();

        /// <summary>
        /// Устанавливает фабрику проектов (для тестирования).
        /// </summary>
        public static void SetFactory(IProjectFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Создаёт проект через текущую фабрику.
        /// </summary>
        public static BaseProject Create(ProjectType type, string name, string author)
        {
            return _factory.CreateProject(type, name, author);
        }
    }
    #endregion
}

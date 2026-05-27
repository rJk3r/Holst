using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Models
{
    public enum ProjectType { Text, Graph, Canvas, Diagram }

    public abstract class BaseProject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Author { get; set; }
        public abstract ProjectType Type { get; }
    }

    #region Text project

    public abstract class DocumentBlock
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    public class HeaderBlock : DocumentBlock
    {
        public int Level { get; set; } // # - 1; ## - 2; ### - 3
        public string Text { get; set; }
    }

    public class ParagraphBlock : DocumentBlock
    {
        public string Text { get; set; }
    }

    public class TextProject : BaseProject
    {
        public override ProjectType Type => ProjectType.Text;
    }

    public class GraphProject : TextProject
    {
        public override ProjectType Type => ProjectType.Graph;
        
        //public List<GraphEdge> Links { get; set; } = new();
    }

    public class GraphEdge
    {
        public string SourceHeaderId { get; set; }
        public string TargetHeaderId { get; set; }
    }
    #endregion

    #region HOLST class / Diagram class
    // TODO: public abstract class vector, primitive & other


    #endregion

    #region ProjectFactory

    public static class ProjectFactory
    {
        public static BaseProject CreateProject(ProjectType type, string name, string author)
        {
            BaseProject project = type switch
            {
                ProjectType.Text => new TextProject(),
                ProjectType.Graph => new GraphProject(),
                //ProjectType.Canvas => new CanvasProject(),
                //ProjectType.Diagram => new DiagramProject(),
                _ => throw new ArgumentException("Неизвестный тип проекта")
            };

            project.Name = name;
            project.Author = author;
            return project;
        }
    }
    #endregion
}
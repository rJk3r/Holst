using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Models
{
    public enum ProjectType { Text, Graph, Canvas, Diagram }

    public class Project
    {
        public Guid ProjectId { get; init; }
        public string ProjectName { get; set; }
        public DateTime Created { get; init; }
        public DateTime LastChange { get; set; }
        public string Author { get; set; } // Может быть null или пустым
        public ProjectType Type { get; init; }

        // Полиморфная ссылка на реальные данные документа
        //public DocumentContent Content { get; set; }
    }
}

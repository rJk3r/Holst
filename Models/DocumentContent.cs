using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Models
{
    public abstract class DocumentContent
    {
        public abstract bool IsEmpty { get; }
    }

    public class TextDocumentContent : DocumentContent
    {
        public string RawText { get; set; } = string.Empty;
        public override bool IsEmpty => string.IsNullOrWhiteSpace(RawText);
    }

    //public class CanvasDocumentContent : DocumentContent
    //{
    //    //Содержит списки слоев, векторов, координат
    //    public List<VectorShape> Shapes { get; set; } = new();
    //    public override bool IsEmpty => Shapes.Count == 0;
    //}

    //public class MindMapDocumentContent : DocumentContent
    //{
    //    public List<MindMapNode> Nodes { get; set; } = new();
    //    public override bool IsEmpty => Nodes.Count == 0;
    //}
}

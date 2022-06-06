using Godot;
using System.Text;
using System.Collections.Generic;

namespace Bentengan.Mcts
{
    public class MonteCarloTreeSearch : Node
    {
        public MctsNode Root;

        public override void _Ready()
        {
            GenerateDummy();
        }

        public string GetMctsPath(MctsNode node)
        {
            var strBuilder = new StringBuilder();
            AppendMctsName(node, strBuilder);

            return strBuilder.ToString();
        }

        private void AppendMctsName(MctsNode node, StringBuilder stringBuilder, string separator = "/")
        {
            if (node.parent != null)
                AppendMctsName(node.parent, stringBuilder, separator);

            stringBuilder.Append(node.name);
            stringBuilder.Append(separator);
        }

        private void GenerateDummy()
        {
            Root = new MctsNode();
            Root.name = "root";

            Root.AddChild("1");
            Root.AddChild("2");
            Root.AddChild("3");
            var child1 = Root.AddChild("4");

            child1.AddChild("5");
            child1.AddChild("7");
            var child2 = child1.AddChild("6");

            var hehe = child2.AddChild("HEHE");

            string path = GetMctsPath(hehe);

            GD.Print(path);
        }
    }

    public class MctsNode
    {
        public string name;

        public float score;
        public int timesVisit;

        public MctsNode parent;
        public List<MctsNode> childs;

        public void SetParent(MctsNode parent)
        {
            this.parent = parent;
        }

        public MctsNode AddChild(string name, float score = 0f, int visit = 0)
        {
            MctsNode node = new MctsNode();
            node.name = name;
            node.score = score;
            node.timesVisit = visit;

            node.SetParent(this);

            if (childs == null)
                childs = new List<MctsNode>();

            childs.Add(node);
            return node;
        }
    }

}
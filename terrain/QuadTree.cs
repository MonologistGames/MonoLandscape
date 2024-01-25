using System.Collections.Generic;

namespace MonoLandscape.Terrain;

public class QuadTree
{
    private List<QuadTreeNode> _nodes = new();
    public QuadTreeNode Root { get; set; } = null;
}
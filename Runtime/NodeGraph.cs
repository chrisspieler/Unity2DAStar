using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class NodeGraph : MonoBehaviour
{
    public Dictionary<Vector2Int, NavNode> Nodes 
    { 
        get
        {
            // Build the node graph if it hasn't been built already.
            if (_nodes.Count == 0 && NavGrid != null)
            {
                BuildNodeGraph();
            }
            return _nodes;
        }
        private set
        {
            _nodes = value;
        }
    }
    private Dictionary<Vector2Int, NavNode> _nodes = new Dictionary<Vector2Int, NavNode>();

    public NavigationGrid NavGrid { get; private set; }

    private void Start()
    {
        NavGrid = GetComponent<NavigationGrid>();
    }

    public PathFinder CreatePathFinder(NavNode goal)
    {
        if (Nodes == null)
        {
            BuildNodeGraph();
        }
        return new PathFinder(this, goal);
    }

    public void BuildNodeGraph()
    {
        Debug.Log("Building node graph.");
        _nodes = new Dictionary<Vector2Int, NavNode>();
        var openCells = NavGrid.GetAllNonCollisionCoordinates();
        Debug.Log($"Node graph non-collision coordinates: {openCells.Count}");
        foreach(var coordinate in openCells)
        {
            var node = new NavNode()
            {
                Neighbors = new List<NavNode>(),
                Position = coordinate,
                WorldPosition = NavGrid.CellToWorld(coordinate)
            };
            _nodes.Add(coordinate, node);
        }
        Debug.Log($"{_nodes.Count} nodes were added");
        // Give each node a reference to each of their neighbors.
        foreach(var node in _nodes)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    if (xOffset == 0 && yOffset == 0)
                    {
                        continue;
                    }
                    var neighborPosition = new Vector2Int(node.Key.x + xOffset, node.Key.y + yOffset);
                    if (_nodes.ContainsKey(neighborPosition))
                    {
                        node.Value.Neighbors.Add(Nodes[neighborPosition]);
                    }
                }
            }
        }
    }
}
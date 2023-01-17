using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PathFinder
{
    public PathFinder(NodeGraph nodes, NavNode goalNode)
    {
        Nodes = nodes;
        _hScores = CalculateHeuristic(Nodes, goalNode.Position);
        GoalNode = goalNode;
    }

    public NavNode GoalNode { get; private set; }
    public NodeGraph Nodes { get; private set; }
    private Dictionary<Vector2Int, float> _hScores = new Dictionary<Vector2Int, float>();

    /// <summary>
    /// Given a NavNode that exists in the NodeGraph referenced by this PathFinder, this method will run an A*
    /// search algorithm to generate a list of worldspace positions that form a contiguous path from the given 
    /// NavNode to goal node referenced in the GoalNode property of this PathFinder. This method will return
    /// true or false depending on whether a valid path was found. If a valid path was found by this method,
    /// foundPath will hold a reference to a the found path.
    /// </summary>
    public bool FindPath(NavNode startNode, out List<Vector2> foundPath)
    {
        // Priority queue that holds a "to-do list" of unexplored neighbors to look at. Don't push to this
        // queue directly- instead use the PushToFrontier function so that scoredMap gets updated.
        var frontier = new MinHeap<ScoredNode>();
        // Associates a NavNode with an object that holds a reference to that NavNode as well as the f-score.
        var scoredMap = new Dictionary<NavNode, ScoredNode>();
        // The f-score is equal to the sum of the heuristic score (hScore) and cost score (gScore). 
        // The start node has a cost of zero, so we only need to add the hScore. Because the Heuristic scores for 
        // each node were already calculated in the constructor, we simply take it from _scores here.
        PushToFrontier(startNode, _hScores[startNode.Position]);
        // The cost score for a node is assumed to be infinite until we've determined that it's less.
        var gScores = new DefaultDictionary<Vector2Int, float>(float.PositiveInfinity);

        gScores.Add(startNode.Position, 0f);

        var cameFrom = new Dictionary<NavNode, NavNode>();

        while (frontier.Count > 0)
        {
            var current = frontier.Pop();
            var node = current.Node;

            if (node.Position == GoalNode.Position)
            {
                foundPath = ReconstructPath(cameFrom);
                return true;
            }

            foreach (var neighbor in node.Neighbors)
            {
                var tentativeGScore = gScores[node.Position];
                if (tentativeGScore < gScores[neighbor.Position])
                {
                    cameFrom[neighbor] = node;
                }
                gScores[neighbor.Position] = tentativeGScore;
                
                var neighborFScore = gScores[neighbor.Position] + _hScores[neighbor.Position];
                if (!scoredMap.ContainsKey(neighbor))
                {
                    PushToFrontier(neighbor, neighborFScore);
                }
                else
                {
                    scoredMap[neighbor].Score = neighborFScore;
                }
            }
        }

        foundPath = new List<Vector2>();
        return false;

        void PushToFrontier(NavNode node, float FScore)
        {
            var scoredNode = new ScoredNode(node, FScore);
            frontier.Push(scoredNode);
            scoredMap[node] = scoredNode;
        }

        List<Vector2> ReconstructPath(Dictionary<NavNode, NavNode> cameFrom)
        {
            var totalPath = new List<Vector2>();
            totalPath.Add(GoalNode.WorldPosition);
            var current = GoalNode;
            while (cameFrom.ContainsKey(current))
            {
                totalPath.Add(current.WorldPosition);
                current = cameFrom[current];
            }
            totalPath.Reverse();
            return totalPath;
        }
    }

    private Dictionary<Vector2Int, float> CalculateHeuristic(NodeGraph nodes, Vector2Int goalPosition)
    {
        var hScores = new Dictionary<Vector2Int, float>();
        foreach(var fromPosition in nodes.Nodes.Keys)
        {
            var offset = fromPosition - goalPosition;
            hScores.Add(fromPosition, offset.sqrMagnitude);
        }
        return hScores;
    }
}
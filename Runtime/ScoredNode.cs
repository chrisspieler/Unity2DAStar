using System;

/// <summary>
/// Simply holds a reference to a NavNode and provides a Score property that is used by
/// the CompareTo method to rank nodes.
/// </summary>
class ScoredNode : IComparable
{
    public ScoredNode(NavNode node, float score)
    {
        Node = node;
        Score = score;
    }
    public NavNode Node { get; set; }
    /// <summary>
    /// A score (such as the f-score used in A*) that is used by the CompareTo to compare this ScoredNode
    /// with another ScoredNode.
    /// </summary>
    public float Score { get; set; }

    public int CompareTo(object obj)
    {
        var other = (ScoredNode)obj;
        if (other.Score > Score)
        {
            return -1;
        }
        else if (other.Score < Score)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
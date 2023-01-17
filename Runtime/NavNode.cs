using System;
using System.Collections.Generic;
using UnityEngine;

public class NavNode
{
    public Vector2 WorldPosition { get; set; }
    public Vector2Int Position { get; set; }
    public List<NavNode> Neighbors { get; set; }
}
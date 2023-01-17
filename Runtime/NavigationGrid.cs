using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Runtime.CompilerServices;

[CustomEditor(typeof(NavigationGrid))]
public class NavigationGridEditor : Editor
{
    public void OnSceneGUI()
    {
        var navGrid = (NavigationGrid)target;
        var bounds = navGrid.NavigationArea;
        Vector3 center = bounds.center;

        EditorGUI.BeginChangeCheck();
        var updatedNavArea = bounds;
        var quadrants = new (int, int)[] { (-1, 1), (1, 1), (1, -1), (-1, -1)};
        foreach(var quadrant in quadrants)
        {
            if (CheckCorner(quadrant.Item1, quadrant.Item2, ref updatedNavArea))
            {
                break;
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Change Navigation Grid Center");
            navGrid.NavigationArea = updatedNavArea;
            navGrid.GenerateGrid();
        }

        bool CheckCorner(int xFactor, int yFactor, ref Bounds newBounds)
        {
            var cornerPosition = center + new Vector3(bounds.extents.x * xFactor, bounds.extents.y * yFactor);
            Handles.color = Color.blue;
            float size = HandleUtility.GetHandleSize(center) * 0.1f;
            Vector3 snap = Vector3.one * navGrid.CellResolution;
            Vector3 newPosition = Handles.FreeMoveHandle(cornerPosition, Quaternion.identity, size, snap, Handles.DotHandleCap);
            if (cornerPosition != newPosition)
            {
                
                Vector3 nw = center + new Vector3(bounds.extents.x * -1, bounds.extents.y * 1);
                Vector3 ne = center + new Vector3(bounds.extents.x * 1, bounds.extents.y * 1);
                Vector3 sw = center + new Vector3(bounds.extents.x * -1, bounds.extents.y * -1);
                Vector3 se = center + new Vector3(bounds.extents.x * 1, bounds.extents.y * -1);
                if (cornerPosition.y != newPosition.y)
                {
                    if (yFactor > 0)
                    {
                        nw.y = newPosition.y;
                        ne.y = newPosition.y;
                    }
                    else
                    {
                        sw.y = newPosition.y;
                        se.y = newPosition.y;
                    }
                }
                if (cornerPosition.x != newPosition.x)
                {
                    if (xFactor > 0)
                    {
                        ne.x = newPosition.x;
                        se.x = newPosition.x;
                    }
                    else
                    {
                        nw.x = newPosition.x;
                        sw.x = newPosition.x;
                    }
                }
                var midX = (ne.x + sw.x) / 2;
                var midY = (ne.y + sw.y) / 2;
                var newCenter = new Vector3(midX, midY, bounds.center.z);
                var newSize = new Vector3(Mathf.Abs(ne.x - nw.x), Mathf.Abs(nw.y - sw.y));
                newBounds = new Bounds(newCenter, newSize);
                return true;
            }

            return false;
        }
    }

    public override void OnInspectorGUI()
    {
        var navGrid = (NavigationGrid)target;
        if (GUILayout.Button("Generate Grid"))
        {
            navGrid.GenerateGrid();
        }
        base.OnInspectorGUI();
    }
}

/// <summary>
/// Defines an the bounds and resolution of an area that will be checked for collisions on a specified
/// collision layer. The existence of collisions is recorded using a BitArray in the Cells property.
/// </summary>
[ExecuteInEditMode]
public class NavigationGrid : MonoBehaviour
{
    /// <summary>
    /// The resolution of this NavigationGrid. Each cell is square, so CellResolution represents the width and
    /// height of a cell in world space.
    /// </summary>
    [field: SerializeField]
    public float CellResolution { get; set; } = 0.1f;
    public int CollisionLayer;
    /// <summary>
    /// A texture that gets updated whenever the grid collisions are calculated. This is used as an overlay
    /// for debugging purposes. The width and height of this texture in texels should match the number of
    /// horizontal and vertical cells in this NavigationGrid.
    /// </summary>
    [field: SerializeField]
    public Texture2D Visualization { get; private set; }
    /// <summary>
    /// A bitmask containing (Width * Height) bits that when set indicate that a collision was found at that coordinate/index
    /// when generating the grid. The indices are row-major X,Y coordinates starting from the bottom row and ascending row-by-row. 
    /// </summary>
    public BitArray Cells { get; private set; }
    [field: SerializeField]
    public Bounds NavigationArea { get; set; }
    /// <summary>
    /// Return the number of cell rows that exist given the current CellResolution and width of NavigationArea.
    /// </summary>
    public int Width => Mathf.FloorToInt(NavigationArea.extents.x * 2 / CellResolution);
    /// <summary>
    /// Return the number of cell columns that exist given the current CellResolution and height of NavigationArea.
    /// </summary>
    public int Height => Mathf.FloorToInt(NavigationArea.extents.y * 2 / CellResolution);

    private void Start()
    {
        GenerateGrid();   
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a green box visualizing NavigationArea.
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(NavigationArea.center, NavigationArea.extents * 2);
        // Invert y extents for scaling the texture otherwise the image will be displayed upside-down.
        var adjustedSize = new Vector2(NavigationArea.extents.x, -NavigationArea.extents.y) * 2;
        /* 
         * The position given for DrawGUITexture seems to be handled as an upper-left corner of the texture,
         * which grows down and to the right, so we push everything up and to the left so that the upper-left 
         * corner of the texture doesn't end up in the exact middle of the actual bounds. In other words,
         * bounds grow from the center outward in all directions, but the GUI texture grows from the upper-left
         * down and right. We adjust for this discrepancy here.
         */
        var adjustedYPosition = NavigationArea.center.y + NavigationArea.extents.y;
        var adjustedXPosition = NavigationArea.center.x - NavigationArea.extents.x;
        var adjustedPosition = new Vector2(adjustedXPosition, adjustedYPosition);
        var imageExtents = new Rect(adjustedPosition, adjustedSize);
        Gizmos.DrawGUITexture(imageExtents, Visualization);
    }

    /// <summary>
    /// The bottom left corner of the NavigationArea in world space.
    /// </summary>
    public Vector2 GridOrigin => NavigationArea.center - NavigationArea.extents;

    public List<Vector2Int> GetAllCollisionCoordinates() => GetAllCellCoordinates(true);
    public List<Vector2Int> GetAllNonCollisionCoordinates() => GetAllCellCoordinates(false);
    private List<Vector2Int> GetAllCellCoordinates(bool collisionFlag)
    {
        var cells = new List<Vector2Int>();
        for (int y = 0; y < Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                if (Cells[y * Width + x] == collisionFlag)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }
        }
        return cells;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CoordinateToIndex(int x, int y) => y * Width + x;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2Int IndexToCoordinate(int index) => new Vector2Int(index % Width, index / Width);



    /// <summary>
    /// Convert a position in world space to discrete coordinates relative to the bottom left corner of NavigationArea.
    /// </summary>
    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        Vector2 localPosition = worldPosition - GridOrigin;
        int xPos = Mathf.FloorToInt(localPosition.x / CellResolution);
        int yPos = Mathf.FloorToInt(localPosition.y / CellResolution);
        return new Vector2Int(xPos, yPos);
    }

    /// <summary>
    /// Convert a grid coordinate position to world space regardless of whether that position actually exists.
    /// </summary>
    /// <param name="cellPosition"></param>
    /// <returns></returns>
    public Vector2 CellToWorld(Vector2Int cellPosition)
    {
        return CellToWorld(cellPosition.x, cellPosition.y);
    }

    public Vector2 CellToWorld(int x, int y)
    {
        var xCenter = GridOrigin.x + (CellResolution * x + CellResolution / 2);
        var yCenter = GridOrigin.y + (CellResolution * y + CellResolution / 2);
        return new Vector2(xCenter, yCenter);
    }

    public void GenerateGrid()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Each cell will be true if there is a collision that prevents pathfinding or false otherwise.
        Cells = new BitArray(Width * Height);

        for (int x = 0; x < Width; x++)
        {
            for(int y = 0; y < Height; y++)
            {
                // The center of the cell at x,y
                var cellCenter = CellToWorld(x, y);
                var collisions = Physics2D.OverlapBoxAll(cellCenter, new Vector3(CellResolution, CellResolution), 0);
                foreach(var collision in collisions)
                {
                    if (collision.gameObject.layer == CollisionLayer)
                    {
                        Cells[CoordinateToIndex(x, y)] = true;
                        break; 
                    }
                }
            }
        }

        Visualization = CreateTexture(Cells);
        sw.Stop();
        Debug.Log($"Generated NavigationGrid with {Width} * {Height} cells in {sw.ElapsedMilliseconds} milliseconds.");
    }

    /// <summary>
    /// Creates a texture that when overlaid on a scene in the Unity editor will visualize the contents of Cells for 
    /// this NavigationGrid.
    /// </summary>
    private Texture2D CreateTexture(BitArray data)
    {
        // Each collision cell is a single pixel.
        var tex = new Texture2D(Width, Height);
        // Make the texture scale up sharply rather than blurring.
        tex.filterMode = FilterMode.Point;
        var alpha = 0.8f;
        var trueColor = new Color(0.1f, 0.8f, 0.1f, alpha);
        var falseColor = new Color(0.1f, 0.5f, 0.1f, alpha);

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var bit = data[CoordinateToIndex(x, y)];
                tex.SetPixel(x, y, bit ? trueColor : falseColor);
            }
        }
        tex.Apply();
        return tex;
    }
}

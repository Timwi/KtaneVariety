using UnityEngine;

public class MazePrefab : MonoBehaviour
{
    public MeshFilter Frame;
    public MeshFilter Back;
    public Mesh[] FrameMeshes;
    public Mesh[] BackMeshes;
    public KMSelectable[] Buttons;  // Up, Right, Down, Left
}

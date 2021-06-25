using UnityEngine;

public class MazePrefab : MonoBehaviour
{
    public MeshFilter Frame;
    public MeshFilter Back;
    public Mesh[] FrameMeshes;
    public Mesh[] BackMeshes;
    public KMSelectable[] Buttons;  // Up, Right, Down, Left
    public Transform[] ButtonPos;  // Up, Right, Down, Left
    public SphereCollider[] ButtonColliders;  // Up, Right, Down, Left
    public GameObject Dot;
    public GameObject Position;
    public MeshRenderer PositionRenderer;
    public Texture[] PositionTextures;  // Shape[3] + 3*Color[3]
}

using UnityEngine;

public class ButtonPrefab : MonoBehaviour
{
    public KMSelectable Button;
    public Transform ButtonParent;
    public MeshRenderer ButtonRenderer;
    public MeshFilter ButtonMesh;
    public MeshFilter ButtonHighlight;
    public Mesh[] Meshes;
    public Material[] Colors;
}

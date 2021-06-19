using UnityEngine;

public class WirePrefab : MonoBehaviour
{
    // These must match the order in WireColor (enum)
    public Material[] WireMaterials;
    public Material CopperMaterial;
    public GameObject Base1;
    public GameObject Base2;
    public KMSelectable Wire;
    public MeshFilter WireMeshFilter;
    public MeshFilter WireHighlightMeshFilter;
    public MeshFilter WireCopperMeshFilter;
    public MeshRenderer WireMeshRenderer;
    public MeshCollider WireCollider;
}

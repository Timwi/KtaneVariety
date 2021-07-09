using UnityEngine;

public class LetterDisplayPrefab : MonoBehaviour
{
    public MeshRenderer[] Segments1;
    public MeshRenderer[] Segments2;
    public MeshRenderer[] Segments3;
    public KMSelectable[] DownButtons;
    public Transform[] DownButtonParents;
    public Material SegmentOn;
    public Material SegmentOff;

    private MeshRenderer[][] _segmentsCache;
    public MeshRenderer[][] Segments { get { return _segmentsCache ?? (_segmentsCache = new[] { Segments1, Segments2, Segments3 }); } }
}

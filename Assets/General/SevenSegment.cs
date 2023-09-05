using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SevenSegment : MonoBehaviour
{
    [SerializeField]
    private Renderer[] _segments;
    public Material OnMaterial, OffMaterial;

    private static readonly Dictionary<int, bool[]> _segmentMappings = "0:1110111|1:0010010|2:1011101|3:1011011|4:0111010|5:1101011|6:1101111|7:1010010|8:1111111|9:1111011"
        .Split('|')
        .ToDictionary(
            s => int.Parse(s.Substring(0, 1)),
            s => s.Substring(2).Select(c => c == '1').ToArray()
        );

    private void Start()
    {
        Debug.Assert(_segments.Length == 7);
        foreach (var s in _segments)
            Debug.Assert(s != null);
    }

    public void SetDigit(int i)
    {
        if (i < 0 || i > 9)
            throw new ArgumentException(nameof(i));

        if (OnMaterial == null)
            throw new UnassignedReferenceException(nameof(OnMaterial));
        if (OffMaterial == null)
            throw new UnassignedReferenceException(nameof(OffMaterial));

        for (int ix = 0; ix < 7; ix++)
            _segments[ix].sharedMaterial = _segmentMappings[i][ix] ? OnMaterial : OffMaterial;
    }
}

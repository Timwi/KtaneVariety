using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Variety;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Variety
/// Created by Timwi
/// </summary>
public class VarietyModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable ModuleSelectable;
    public KMAudio Audio;

    public DummyPrefab DummyTemplate;
    public WirePrefab WireTemplate;
    public KeyPrefab KeyTemplate;
    public MazePrefab MazeTemplate;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Item[] _items;
    private int _state;

    public const int W = 13;                // Number of slots in X direction
    public const int H = 10;                // Number of slots in Y direction
    public const float Width = .15f;      // Width of the field on the module
    public const float Height = .1125f; // Height of the field on the module
    public const float YOffset = -.02f;  // Vertical positioning of the field
    public const float CellWidth = Width / (W - 1);
    public const float CellHeight = Height / (H - 1);

    struct ItemFactoryInfo
    {
        public ItemFactoryInfo(int weight, ItemFactory factory)
        {
            Weight = weight;
            Factory = factory;
        }

        public int Weight { get; private set; }
        public ItemFactory Factory { get; private set; }
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var factories = new List<ItemFactoryInfo>
        {
            new ItemFactoryInfo(0, new DummyFactory()),
            new ItemFactoryInfo(1, new WireFactory()),
            new ItemFactoryInfo(2, new KeyFactory()),
            new ItemFactoryInfo(2, new MazeFactory())
        };

        var takens = new HashSet<object>();
        var children = new KMSelectable[W * H];
        while (factories.Count > 0)
        {
            var cumulativeWeight = Ut.NewArray(factories.Count, i => factories.Take(i + 1).Sum(fi => fi.Weight));
            var rnd = Rnd.Range(0, cumulativeWeight.Last());
            var fIx = cumulativeWeight.Last() == 0 ? 0 : cumulativeWeight.IndexOf(w => rnd < w);
            var item = factories[fIx].Factory.Generate(this, takens);
            if (item == null)
                factories.RemoveAt(fIx);
            else
            {
                Debug.LogFormat(@"[Variety #{0}] Placed {1}", _moduleId, item);
                foreach (var inf in item.SetUp())
                {
                    inf.Selectable.Parent = ModuleSelectable;
                    for (var i = 0; i < inf.Cells.Length; i++)
                        children[inf.Cells[i]] = inf.Selectable;
                }
                factories[fIx] = new ItemFactoryInfo(Math.Max(1, factories[fIx].Weight - 1), factories[fIx].Factory);
            }
        }
        ModuleSelectable.Children = children;
        ModuleSelectable.UpdateChildren();
    }
}

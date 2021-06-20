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
    public KMRuleSeedable RuleSeedable;

    public DummyPrefab DummyTemplate;
    public WirePrefab WireTemplate;
    public KeyPrefab KeyTemplate;
    public MazePrefab MazeTemplate;
    public SliderPrefab SliderTemplate;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Item[] _items;
    private int _state;
    private int[] _expectedStates;

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

    private object[] _flavorOrder;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var ruleSeedRnd = RuleSeedable.GetRNG();

        var factories = new List<ItemFactoryInfo>
        {
            new ItemFactoryInfo(0, new DummyFactory()),
            new ItemFactoryInfo(1, new WireFactory()),
            new ItemFactoryInfo(2, new KeyFactory()),
            new ItemFactoryInfo(2, new SliderFactory()),
            new ItemFactoryInfo(3, new MazeFactory(ruleSeedRnd))
        };

        _flavorOrder = ruleSeedRnd.ShuffleFisherYates(factories.SelectMany(inf => inf.Factory.Flavors).ToArray());

        var takens = new HashSet<object>();
        var children = new KMSelectable[W * H];
        var items = new List<Item>();
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
                    children[inf.Cell] = inf.Selectable;
                }
                factories[fIx] = new ItemFactoryInfo(Math.Max(1, factories[fIx].Weight - 1), factories[fIx].Factory);
                items.Add(item);
            }
        }
        ModuleSelectable.Children = children;
        ModuleSelectable.UpdateChildren();

        _items = items.OrderBy(item => Array.IndexOf(_flavorOrder, item.Flavor)).ToArray();

        var maxState = _items.Aggregate(1, (p, n) => p * n.NumStates);
        var state = _state = Rnd.Range(0, maxState);
        Debug.LogFormat(@"[Variety #{0}] State: {1}", _moduleId, _state);
        Debug.LogFormat(@"[Variety #{0}] Expected actions:", _moduleId);
        _expectedStates = new int[_items.Length];
        for (var i = 0; i < _items.Length; i++)
        {
            if (_items[i].NumStates < 2)
                continue;
            _items[i].StateSet = StateSet(i);
            _expectedStates[i] = state % _items[i].NumStates;
            state /= _items[i].NumStates;
            Debug.LogFormat(@"[Variety #{0}] {1}: {2}", _moduleId, _items[i], _items[i].DescribeState(_expectedStates[i], isSolution: true));
        }
    }

    private Action<int> StateSet(int itemIx)
    {
        return delegate (int newState)
        {
            var i = 0;
            for (; i < itemIx; i++)
            {
                if (!_items[i].IsStuck && _items[i].State != _expectedStates[i])
                {
                    Debug.LogFormat(@"[Variety #{0}] You received a strike because you changed “{1}” to “{2}” before changing “{3}” to “{4}”.",
                        _moduleId, _items[itemIx], _items[itemIx].DescribeState(newState), _items[i], _items[i].DescribeState(_expectedStates[i]));
                    _items[i].Checked();
                    Module.HandleStrike();
                    return;
                }
            }

            for (; i < _items.Length; i++)
                if (_items[i].State != _expectedStates[i])
                    return;

            Debug.LogFormat(@"[Variety #{0}] All done!", _moduleId);
            Module.HandlePass();
        };
    }
}

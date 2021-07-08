using System;
using System.Collections;
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
    public TextMesh StateDisplay;

    public DummyPrefab DummyTemplate;
    public WirePrefab WireTemplate;
    public KeyPrefab KeyTemplate;
    public MazePrefab MazeTemplate;
    public SliderPrefab SliderTemplate;
    public KeypadPrefab KeypadTemplate;
    public KnobPrefab KnobTemplate;
    public DigitDisplayPrefab DigitDisplayTemplate;
    public SwitchPrefab SwitchTemplate;
    public LetterDisplayPrefab LetterDisplayTemplate;
    public BrailleDisplayPrefab BrailleDisplayTemplate;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Item[] _items;
    private int[] _expectedStates;

    public int ModuleID { get { return _moduleId; } }

    public const int W = 10;                // Number of slots in X direction
    public const int H = 8;                // Number of slots in Y direction
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
    private bool _isSolved;

    static List<T> makeList<T>(params T[] items) { return new List<T>(items); }

    void Awake()
    {
        _moduleId = _moduleIdCounter++;

        var ruleSeedRnd = RuleSeedable.GetRNG();

        var factories = makeList(
            new ItemFactoryInfo(1, new WireFactory(ruleSeedRnd)),
            new ItemFactoryInfo(2, new KeyFactory()),
            new ItemFactoryInfo(2, new SliderFactory()),
            new ItemFactoryInfo(5, new KnobFactory()),
            new ItemFactoryInfo(5, new SwitchFactory()),
            new ItemFactoryInfo(7, new BrailleDisplayFactory()),
            new ItemFactoryInfo(7, new DigitDisplayFactory()),
            new ItemFactoryInfo(8, new KeypadFactory()),
            new ItemFactoryInfo(10, new MazeFactory(ruleSeedRnd)),
            new ItemFactoryInfo(10, new LetterDisplayFactory()));

        _flavorOrder = factories.SelectMany(inf => inf.Factory.Flavors).ToArray();
        ruleSeedRnd.ShuffleFisherYates(_flavorOrder);
        Debug.LogFormat("<Variety #{0}> Flavour order:\n{1}", _moduleId, _flavorOrder.Join("\n"));

        // Decide what’s going to be on the module
        var takens = new HashSet<object>();
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
                factories[fIx] = new ItemFactoryInfo(1, factories[fIx].Factory);
                items.Add(item);
            }
        }

        // Generate the game objects on the module
        var children = new KMSelectable[W * H];
        for (var i = 0; i < items.Count; i++)
            foreach (var inf in items[i].SetUp())
            {
                inf.Selectable.Parent = ModuleSelectable;
                children[inf.Cell] = inf.Selectable;
            }
        ModuleSelectable.Children = children;
        ModuleSelectable.ChildRowLength = W;
        ModuleSelectable.UpdateChildren();

#if UNITY_EDITOR
        for (var cell = 0; cell < W * H; cell++)
        {
            var dummy = Instantiate(DummyTemplate, transform);
            dummy.transform.localPosition = new Vector3(GetX(cell), .01501f, GetY(cell));
            dummy.transform.localEulerAngles = new Vector3(90, 0, 0);
            dummy.Renderer.sharedMaterial = takens.Contains(cell) ? dummy.Black : dummy.White;
        }
#endif

        items.Sort((a, b) => Array.IndexOf(_flavorOrder, a.Flavor).CompareTo(Array.IndexOf(_flavorOrder, b.Flavor)));
        StartCoroutine(AfterAwake(items));
    }

    public static float GetX(int ix) { return -Width / 2 + (ix % W) * CellWidth; }
    public static float GetY(int ix) { return Height / 2 - (ix / W) * CellHeight + YOffset; }

    private IEnumerator AfterAwake(List<Item> items)
    {
        yield return null;

        _expectedStates = new int[items.Count];

        // Start with a random state number and find out what it would do
        tryAgain:
        ulong state = Enumerable.Range(0, 8).Aggregate(0UL, (p, n) => (p << 8) | (uint) Rnd.Range(0, 256)) % 1000000000000UL;
        ulong reconstructedState = 0UL;
        ulong mult = 1UL;

        var remainingItems = items.ToList();
        var itemProcessingOrder = new List<Item>();

        while (remainingItems.Count > 0)
        {
            var itemIx = (int) (state % (ulong) remainingItems.Count);
            state /= (ulong) remainingItems.Count;
            reconstructedState += mult * (ulong) itemIx;
            mult *= (ulong) remainingItems.Count;
            var item = remainingItems[itemIx];
            remainingItems.RemoveAt(itemIx);
            if (!item.DecideStates(itemProcessingOrder.Count(priorItem => priorItem.CanProvideStage)))
                goto tryAgain;

            var itemState = (int) (state % (ulong) item.NumStates);
            state /= (ulong) item.NumStates;
            reconstructedState += mult * (ulong) itemState;
            mult *= (ulong) item.NumStates;

            _expectedStates[itemProcessingOrder.Count] = itemState;
            itemProcessingOrder.Add(item);
        }
        var stateStr = reconstructedState.ToString();
        if (stateStr.Length > 12)
            throw new InvalidOperationException();

        Debug.LogFormat(@"[Variety #{0}] State: {1}", _moduleId, stateStr);
        StateDisplay.text = stateStr;

        _items = itemProcessingOrder.ToArray();

        Debug.LogFormat(@"[Variety #{0}] Expected actions:", _moduleId);
        ulong maximum = 1UL;
        for (var i = 0; i < _items.Length; i++)
        {
            var remainingItemsCount = _items.Length - i;
            var itemIx = (int) (reconstructedState % (ulong) remainingItemsCount);
            Debug.LogFormat(@"[Variety #{0}] {1} % {2} = {3} = {4} ({5} states)", _moduleId, reconstructedState, remainingItemsCount, itemIx, _items[i], _items[i].NumStates);
            reconstructedState /= (ulong) remainingItemsCount;

            Debug.LogFormat(@"[Variety #{0}] {1} % {2} = {3} = {4}", _moduleId, reconstructedState, _items[i].NumStates, _expectedStates[i], _items[i].DescribeSolutionState(_expectedStates[i]));
            reconstructedState /= (ulong) _items[i].NumStates;

            _items[i].StateSet = StateSet(i);
            maximum *= (ulong) remainingItemsCount;
            maximum *= (ulong) _items[i].NumStates;
        }
        Debug.LogWarningFormat(@"<Variety #{0}> Maximum: {1} ({2} digits) ({3})", _moduleId, maximum, maximum.ToString().Length, ulong.MaxValue.ToString().Length);
    }

    private Action<int> StateSet(int itemIx)
    {
        return delegate (int newState)
        {
            if (newState == -1)
                return;

            if (_items[itemIx].CanProvideStage)
            {
                var stageItemIndex = _items.Where(item => item.CanProvideStage).IndexOf(item => item == _items[itemIx]);
                for (var ix = itemIx + 1; ix < _items.Length; ix++)
                    _items[ix].ReceiveItemChange(stageItemIndex);
            }

            Debug.LogFormat("<Variety #{0}> States:\n{1}", _moduleId, _items.Select((item, ix) => string.Format("{0}: {4}, desired={1}, actual={2}, stuck={3}", ix, _expectedStates[ix], item.State, item.IsStuck, item)).Join("\n"));

            if (_isSolved)
                return;

            var i = 0;
            for (; i < itemIx; i++)
            {
                if (!_items[i].IsStuck && _items[i].State != _expectedStates[i])
                {
                    Debug.LogFormat(@"[Variety #{0}] You received a strike when {1} because {2}.",
                        _moduleId,
                        _items[itemIx].DescribeWhatUserDid(),
                        _items[i].DescribeWhatUserShouldHaveDone(_expectedStates[i]));
                    _items[i].Checked();
                    Module.HandleStrike();
                    return;
                }
            }

            for (; i < _items.Length; i++)
                if (_items[i].State != _expectedStates[i] && !_items[i].IsStuck)
                    return;

            Debug.LogFormat(@"[Variety #{0}] Module solved.", _moduleId);
            Module.HandlePass();
            _isSolved = true;
        };
    }
}

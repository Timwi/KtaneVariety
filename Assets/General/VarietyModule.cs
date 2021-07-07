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
            new ItemFactoryInfo(7, new DigitDisplayFactory()),
            new ItemFactoryInfo(5, new SwitchFactory()),
            new ItemFactoryInfo(8, new KeypadFactory()),
            new ItemFactoryInfo(10, new MazeFactory(ruleSeedRnd)),
            new ItemFactoryInfo(10, new LetterDisplayFactory()));

        _flavorOrder = factories.SelectMany(inf => inf.Factory.Flavors).ToArray();
        ruleSeedRnd.ShuffleFisherYates(_flavorOrder);
        Debug.LogFormat("<Variety #{0}> Flavour order:\n{1}", _moduleId, _flavorOrder.Join("\n"));

        // Decide what’s going to be on the module
        var iterations = 0;
        tryAgain:
        iterations++;

        var remainingFactories = factories.ToList();
        var takens = new HashSet<object>();
        var items = new List<Item>();
        while (remainingFactories.Count > 0)
        {
            var cumulativeWeight = Ut.NewArray(remainingFactories.Count, i => remainingFactories.Take(i + 1).Sum(fi => fi.Weight));
            var rnd = Rnd.Range(0, cumulativeWeight.Last());
            var fIx = cumulativeWeight.Last() == 0 ? 0 : cumulativeWeight.IndexOf(w => rnd < w);
            var item = remainingFactories[fIx].Factory.Generate(this, takens);
            if (item == null)
                remainingFactories.RemoveAt(fIx);
            else
            {
                remainingFactories[fIx] = new ItemFactoryInfo(1, remainingFactories[fIx].Factory);
                items.Add(item);
            }
        }

        // Decide on the order in which the user must solve the items
        items.Shuffle();

        // Figure out how many states each item can have
        for (var i = 0; i < items.Count; i++)
            if (!items[i].DecideStates(items.Take(i).Count(item => item.CanProvideStage)))
                goto tryAgain;
        _items = items.ToArray();

        // Decide on the goal states and calculate the overall state number
        _expectedStates = _items.Select(item => Rnd.Range(0, item.NumStates)).ToArray();
        var itemsInFlavorOrder = _items.OrderBy(item => Array.IndexOf(_flavorOrder, item.Flavor)).ToList();
        ulong state = 0;
        ulong mult = 1;
        for (var i = 0; i < _items.Length; i++)
        {
            var itemIx = itemsInFlavorOrder.IndexOf(_items[i]);
            state += mult * (ulong) itemIx;
            mult *= (ulong) itemsInFlavorOrder.Count;
            itemsInFlavorOrder.RemoveAt(itemIx);
            state += mult * (ulong) _expectedStates[i];
            mult *= (ulong) _items[i].NumStates;
        }
        if (state.ToString().Length > 12)
            goto tryAgain;

        Debug.LogFormat(@"<Variety #{0}> Iterations: {1}", _moduleId, iterations);
        Debug.LogFormat(@"[Variety #{0}] State: {1}", _moduleId, state);
        StateDisplay.text = state.ToString();

        // Generate the game objects on the module
        var children = new KMSelectable[W * H];
        for (var i = 0; i < _items.Length; i++)
            foreach (var inf in _items[i].SetUp())
            {
                inf.Selectable.Parent = ModuleSelectable;
                children[inf.Cell] = inf.Selectable;
            }
        ModuleSelectable.Children = children;
        ModuleSelectable.ChildRowLength = W;

#if UNITY_EDITOR
        for (var cell = 0; cell < W * H; cell++)
        {
            var dummy = Instantiate(DummyTemplate, transform);
            dummy.transform.localPosition = new Vector3(GetX(cell), .01501f, GetY(cell));
            dummy.transform.localEulerAngles = new Vector3(90, 0, 0);
            dummy.Renderer.sharedMaterial = takens.Contains(cell) ? dummy.Black : dummy.White;
        }
#endif

        StartCoroutine(AfterAwake());
    }

    public static float GetX(int ix) { return -Width / 2 + (ix % W) * CellWidth; }
    public static float GetY(int ix) { return Height / 2 - (ix / W) * CellHeight + YOffset; }

    private IEnumerator AfterAwake()
    {
        yield return null;

        Debug.LogFormat(@"[Variety #{0}] Expected actions:", _moduleId);
        for (var i = 0; i < _items.Length; i++)
        {
            _items[i].ObtainEdgework();
            _items[i].StateSet = StateSet(i);
            Debug.LogFormat(@"[Variety #{0}] {1}", _moduleId, _items[i].DescribeSolutionState(_expectedStates[i]));
        }
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

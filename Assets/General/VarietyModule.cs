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

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Item[] _items;
    private int[] _expectedStates;

    public int ModuleID { get { return _moduleId; } }

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
    private bool _isSolved;

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
            new ItemFactoryInfo(2, new KnobFactory()),
            new ItemFactoryInfo(2, new DigitDisplayFactory()),
            new ItemFactoryInfo(2, new SwitchFactory()),
            new ItemFactoryInfo(3, new KeypadFactory()),
            new ItemFactoryInfo(3, new MazeFactory(ruleSeedRnd))
        };
        _flavorOrder = ruleSeedRnd.ShuffleFisherYates(factories.SelectMany(inf => inf.Factory.Flavors).ToArray());

        // Decide what’s going to be on the module
        var iterations = 0;
        tryAgain:
        iterations++;
        Debug.LogFormat(@"<Variety #{0}> Iteration {1}", _moduleId, iterations);
        if (iterations > 100)
            throw new InvalidOperationException();

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
                remainingFactories[fIx] = new ItemFactoryInfo(Math.Max(1, remainingFactories[fIx].Weight - 1), remainingFactories[fIx].Factory);
                items.Add(item);
            }
        }

        // Decide on the order in which the user must solve the items
        items.Shuffle();

        // Figure out how many states each item can have
        for (var i = 0; i < items.Count; i++)
            if (!items[i].DecideStates(items.Take(i).Count(item => item.CanProvideStage)))
                goto tryAgain;

        // Decide on the goal states and calculate the overall state number
        _items = items.Where(item => !(item is Dummy)).ToArray();
        _expectedStates = _items.Select(item => Rnd.Range(0, item.NumStates)).ToArray();
        var itemsInFlavorOrder = _items.OrderBy(item => Array.IndexOf(_flavorOrder, item.Flavor)).ToList();
        ulong state = 0;
        ulong mult = 1;
        for (var i = 0; i < _items.Length; i++)
        {
            var itemIx = itemsInFlavorOrder.IndexOf(_items[i]);
            state += mult * (ulong) itemIx;
            mult *= (ulong) itemsInFlavorOrder.Count;
            //Debug.LogFormat(@"<Variety #{0}> items = {1}, mult = {2}", _moduleId, itemsInFlavorOrder.Count, mult);
            itemsInFlavorOrder.RemoveAt(itemIx);
            state += mult * (ulong) _expectedStates[i];
            mult *= (ulong) _items[i].NumStates;
            //Debug.LogFormat(@"<Variety #{0}> states = {1}, mult = {2}", _moduleId, _items[i].NumStates, mult);
        }
        if (state.ToString().Length > 17)
            goto tryAgain;

        Debug.LogFormat(@"[Variety #{0}] State: {1}", _moduleId, state);
        StateDisplay.text = state.ToString();


        // Generate the game objects on the module
        var children = new KMSelectable[W * H];
        foreach (var item in items)
            foreach (var inf in item.SetUp())
            {
                inf.Selectable.Parent = ModuleSelectable;
                children[inf.Cell] = inf.Selectable;
            }
        ModuleSelectable.Children = children;
        ModuleSelectable.UpdateChildren();

        Debug.LogFormat(@"[Variety #{0}] Expected actions:", _moduleId);
        for (var i = 0; i < _items.Length; i++)
        {
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

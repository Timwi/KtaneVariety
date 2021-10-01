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
    private static readonly string[] _segmentMap = new[] { "1111101", "1001000", "0111011", "1011011", "1001110", "1010111", "1110111", "1001001", "1111111", "1011111" };

    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable ModuleSelectable;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public MeshRenderer[] StateSegments;
    public Material StateSegmentOn;
    public Material StateSegmentOff;

    public DummyPrefab DummyTemplate;
    public WirePrefab WireTemplate;
    public KeyPrefab KeyTemplate;
    public MazePrefab MazeTemplate;
    public SliderPrefab SliderTemplate;
    public KeypadPrefab KeypadTemplate;
    public ColoredKeypadPrefab ColoredKeypadTemplate;
    public KnobPrefab KnobTemplate;
    public DigitDisplayPrefab DigitDisplayTemplate;
    public SwitchPrefab SwitchTemplate;
    public LetterDisplayPrefab LetterDisplayTemplate;
    public BrailleDisplayPrefab BrailleDisplayTemplate;
    public ButtonPrefab ButtonTemplate;
    public LedPrefab LedTemplate;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Item[] _items;
    private int[] _expectedStates;
    private int _lastTimerSeconds;
    private int _timerTicks;

    public int ModuleID { get { return _moduleId; } }
    public int TimerTicks { get { return _timerTicks; } }

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
            new ItemFactoryInfo(2, new LedFactory(ruleSeedRnd)),
            new ItemFactoryInfo(2, new SwitchFactory()),
            new ItemFactoryInfo(3, new KnobFactory()),
            new ItemFactoryInfo(4, new ButtonFactory(ruleSeedRnd)),
            new ItemFactoryInfo(5, new BrailleDisplayFactory()),
            new ItemFactoryInfo(6, new DigitDisplayFactory()),
            new ItemFactoryInfo(7, new SliderFactory()),
            new ItemFactoryInfo(7, new KeypadFactory()),
            new ItemFactoryInfo(7, new ColoredKeypadFactory(ruleSeedRnd)),
            new ItemFactoryInfo(10, new MazeFactory(ruleSeedRnd)),
            new ItemFactoryInfo(10, new LetterDisplayFactory()));

        _flavorOrder = factories.SelectMany(inf => inf.Factory.Flavors).ToArray();
        Debug.LogFormat("<Variety #{0}> Flavour order:\n{1}", _moduleId, _flavorOrder.Join("\n"));
        ruleSeedRnd.ShuffleFisherYates(_flavorOrder);
        ruleSeedRnd.ShuffleFisherYates(_flavorOrder);

        // Decide what’s going to be on the module
        var iterations = 0;
        tryAgain:
        iterations++;
        var remainingFactories = factories.ToList();
        var takens = new HashSet<object>();
        var items = new List<Item>();
        while (remainingFactories.Count > 0 && items.Count < 10)
        {
            var cumulativeWeight = Ut.NewArray(remainingFactories.Count, i => remainingFactories.Take(i + 1).Sum(fi => fi.Weight));
            var rnd = Rnd.Range(0, cumulativeWeight.Last());
            var fIx = cumulativeWeight.Last() == 0 ? 0 : cumulativeWeight.IndexOf(w => rnd < w);
            var item = remainingFactories[fIx].Factory.Generate(this, takens);
            if (item == null)
                remainingFactories.RemoveAt(fIx);
            else
            {
                remainingFactories[fIx] = new ItemFactoryInfo(Math.Max(1, remainingFactories[fIx].Weight - 2), remainingFactories[fIx].Factory);
                items.Add(item);
            }
        }
        if (items.Count != 10 && iterations < 100)
            goto tryAgain;

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
        //for (var cell = 0; cell < W * H; cell++)
        //{
        //    var dummy = Instantiate(DummyTemplate, transform);
        //    dummy.transform.localPosition = new Vector3(GetX(cell), .01501f, GetY(cell));
        //    dummy.transform.localEulerAngles = new Vector3(90, 0, 0);
        //    dummy.Renderer.sharedMaterial = takens.Contains(cell) ? dummy.Black : dummy.White;
        //}
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
        var fewestZeros = int.MaxValue;
        List<Item> itemsWithFewestZeros = null;
        int[] expectedStatesWithFewestZeros = null;
        ulong stateWithFewestZeros = 0;

        for (var i = 0; i < 100 || itemsWithFewestZeros == null; i++)
        {
            ulong state = Enumerable.Range(0, 8).Aggregate(0UL, (p, n) => (p << 8) | (uint) Rnd.Range(0, 256)) % 1000000000000UL;
            ulong reconstructedState = 0UL;
            ulong mult = 1UL;

            var remainingItems = items.ToList();
            var itemProcessingOrder = new List<Item>();
            var expectedStates = new int[items.Count];

            while (remainingItems.Count > 0)
            {
                var itemIx = (int) (state % (ulong) remainingItems.Count);
                state /= (ulong) remainingItems.Count;
                reconstructedState += mult * (ulong) itemIx;
                mult *= (ulong) remainingItems.Count;
                var item = remainingItems[itemIx];
                remainingItems.RemoveAt(itemIx);
                if (!item.DecideStates(itemProcessingOrder.Count(priorItem => priorItem.CanProvideStage)))
                    goto busted;

                var itemState = (int) (state % (ulong) item.NumStates);
                state /= (ulong) item.NumStates;
                reconstructedState += mult * (ulong) itemState;
                mult *= (ulong) item.NumStates;

                expectedStates[itemProcessingOrder.Count] = itemState;
                itemProcessingOrder.Add(item);
            }

            var numZeros = expectedStates.Reverse().TakeWhile(st => st == 0).Count();
            if (numZeros < fewestZeros)
            {
                fewestZeros = numZeros;
                itemsWithFewestZeros = itemProcessingOrder;
                expectedStatesWithFewestZeros = expectedStates;
                stateWithFewestZeros = reconstructedState;

                if (numZeros == 0)
                    break;
            }

            busted:;
        }

        var stateStr = stateWithFewestZeros.ToString();
        Debug.LogFormat(@"[Variety #{0}] State: {1}", _moduleId, stateStr);
        for (var digit = 0; digit < 12; digit++)
            for (var segment = 0; segment < 7; segment++)
                StateSegments[digit * 7 + segment].sharedMaterial = digit >= stateStr.Length || _segmentMap[stateStr[stateStr.Length - 1 - digit] - '0'][segment] == '0' ? StateSegmentOff : StateSegmentOn;

        _items = itemsWithFewestZeros.ToArray();
        _expectedStates = expectedStatesWithFewestZeros;
        var finalState = stateWithFewestZeros;

        Debug.LogFormat(@"[Variety #{0}] Expected actions:", _moduleId);
        for (var i = 0; i < _items.Length; i++)
        {
            var remainingItemsCount = _items.Length - i;
            Debug.LogFormat(@"[Variety #{0}] {1} % {2} = {3} = {4} ({5} states)", _moduleId, finalState, remainingItemsCount, (int) (finalState % (ulong) remainingItemsCount), _items[i], _items[i].NumStates);
            finalState /= (ulong) remainingItemsCount;

            _items[i].DecideStates(_items.Take(i).Count(priorItem => priorItem.CanProvideStage));
            Debug.LogFormat(@"[Variety #{0}] {1} % {2} = {3} = {4}", _moduleId, finalState, _items[i].NumStates, _expectedStates[i], _items[i].DescribeSolutionState(_expectedStates[i]));
            //Debug.LogFormat(@"[Variety #{0}] {4}", _moduleId, reconstructedState, _items[i].NumStates, _expectedStates[i], _items[i].DescribeSolutionState(_expectedStates[i]));
            finalState /= (ulong) _items[i].NumStates;

            _items[i].StateSet = StateSet(i);
        }
    }

    private Action<int> StateSet(int itemIx)
    {
        return delegate (int newState)
        {
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

    private Coroutine _movingButton = null;
    private Transform _movingButtonBtn = null;
    private Vector3 _movingButtonFinal;

    public void MoveButton(Transform button, float amount, ButtonMoveType type)
    {
        if (_movingButton != null)
        {
            Module.StopCoroutine(_movingButton);
            _movingButtonBtn.localPosition = _movingButtonFinal;
        }
        _movingButton = Module.StartCoroutine(moveButton(button, amount, type));
    }

    private IEnumerator moveButton(Transform button, float amount, ButtonMoveType type)
    {
        _movingButtonBtn = button;
        _movingButtonFinal = type == ButtonMoveType.Down ? new Vector3(0, -amount, 0) : new Vector3(0, 0, 0);

        var duration = .1f;
        var elapsed = 0f;

        if (type == ButtonMoveType.Down || type == ButtonMoveType.DownThenUp)
        {
            while (elapsed < duration)
            {
                button.localPosition = new Vector3(0, Easing.OutQuad(elapsed, 0, -amount, duration), 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            button.localPosition = new Vector3(0, -amount, 0);
        }
        if (type == ButtonMoveType.Up || type == ButtonMoveType.DownThenUp)
        {
            elapsed = 0f;
            while (elapsed < duration)
            {
                button.localPosition = new Vector3(0, Easing.OutQuad(elapsed, -amount, 0, duration), 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            button.localPosition = new Vector3(0, 0, 0);
        }

        _movingButton = null;
        _movingButtonBtn = null;
    }

    void Update()
    {
        var seconds = (int) Bomb.GetTime() % 60;
        if (seconds != _lastTimerSeconds)
        {
            _timerTicks++;
            _lastTimerSeconds = seconds;
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = new[]
    {
        "!{0} horiz/vert 0 [set horizontal/vertical slider]",
        "!{0} key 0 [turn the key-in-lock at last timer digit]",
        "!{0} 1x3 keys 012 [press keys on the 1×3 white keypad in that order]",
        "!{0} red keys 01 [press those keys on the red keypad]",
        "!{0} knob 0 [turn knob to that many tickmarks from north]",
        "!{0} red button mash 3 [mash the red button that many times]",
        "!{0} red button hold 2 [hold the red button over that many timer ticks]",
        "!{0} digit 0 [set the digit display]",
        "!{0} cut blue [cut a wire]",
        "!{0} led white [set the LED to white]",
        "!{0} led reset [show flashing colors again]",
        "!{0} red switch 0 [toggle red switch to up]",
        "!{0} letters cycle [cycle each letter slot]",
        "!{0} letters ACE [set letter display]",
        "!{0} braille 125 [set Braille display]",
        "!{0} 3x3 maze UDLR [make moves in the 3×3 maze]"
    }.Join(" | ");
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var ret = _items.Select(item => item.ProcessTwitchCommand(command)).FirstOrDefault(result => result != null);
        if (ret != null)
        {
            yield return null;
            while (ret.MoveNext())
                yield return ret.Current;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (var i = 0; i < _items.Length; i++)
            if (!_items[i].IsStuck && _items[i].State != _expectedStates[i])
            {
                foreach (var obj in _items[i].TwitchHandleForcedSolve(_expectedStates[i]))
                    yield return obj;
                yield return new WaitForSeconds(.25f);
            }
    }
}

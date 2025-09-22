using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = System.Random;

namespace Variety
{
    public class Timer : Item
    {
        public int Cell { get; private set; }
        public TimerType FlavorType { get; private set; }
        public int NumPositions { get; private set; }

        private static readonly int[] Primes = new int[] { 2, 3, 5, 7 };

        public Timer(VarietyModule module, int cell, TimerType flavor, int A, int B) : base(module, new int[] { cell })
        {
            Cell = cell;
            FlavorType = flavor;
            _a = Primes[A];
            _b = Primes[B];
            NumPositions = _a * _b;
            SetState(-1, automatic: true);
        }

        public override void OnActivate()
        {
            _running = true;
            _timer = _prefab.StartCoroutine(RunTimer());
            _prefab.StartCoroutine(RunTimerColon());
            _active = true;
        }

        public override int NumStates => NumPositions;
        public override object Flavor => FlavorType;

        private readonly int _a, _b;
        private int _displayedTime;
        private bool _running, _active;
        private TimerPrefab _prefab;
        private Coroutine _timer;

        public override IEnumerable<ItemSelectable> SetUp(Random rnd)
        {
            _prefab = Object.Instantiate(Module.TimerTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .015f, GetYOfCellRect(Cells[0], 2));
            _prefab.transform.localScale = Vector3.one;
            _prefab.Selectable.OnInteract += Press;
            yield return new ItemSelectable(_prefab.Selectable, Cells[0]);
        }

        private bool Press()
        {
            _prefab.Selectable.AddInteractionPunch(.5f);
            Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _prefab.transform);

            if (!_active)
                return false;

            if (!_running)
            {
                _running = true;
                SetState(-1);
                _timer = _prefab.StartCoroutine(RunTimer());
            }
            else
            {
                _running = false;
                SetState(_displayedTime);
                _prefab.StopCoroutine(_timer);
            }
            return false;
        }

        private IEnumerator RunTimer()
        {
            float startTime = Time.time - _displayedTime;
            while (true)
            {
                var t = (int) (Time.time - startTime) % NumPositions;
                if (FlavorType == TimerType.Descending)
                    t = NumPositions - 1 - t;
                _displayedTime = t;
                _prefab.LeftDigit.SetDigit(t / _b);
                _prefab.RightDigit.SetDigit(t % _b);
                yield return null;
            }
        }

        private IEnumerator RunTimerColon()
        {
            const float delay = 0.66f;
            while (true)
            {
                _prefab.Colon.color = new Color32(0xDF, 0xF3, 0xFF, 0xFF);
                yield return new WaitForSeconds(delay);
                _prefab.Colon.color = new Color32(0x29, 0x2E, 0x31, 0xFF);
                yield return new WaitForSeconds(delay);
            }
        }

        private string FormatTime(int t) => $"{t / _b}:{t % _b}";
        public override string DescribeSolutionState(int state) => $"set the {FlavorType.ToString().ToLowerInvariant()} timer to {FormatTime(state)}";
        public override string DescribeWhatUserDid() => _running
            ? $"you left the {FlavorType.ToString().ToLowerInvariant()} timer running"
            : $"you set the {FlavorType.ToString().ToLowerInvariant()} timer to {FormatTime(State)}";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have set the {FlavorType.ToString().ToLowerInvariant()} timer to {FormatTime(desiredState)} ({(_running ? "you left it running" : "instead of " + FormatTime(State))})";

        public override string ToString() => $"{FlavorType.ToString().ToLowerInvariant()} timer ({_a}×{_b})";

        public override string TwitchHelpMessage => "!{0} ascending timer 02 [stops the timer at that value] | !{0} ascending timer reset [restarts the timer running]";

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var flavorRegex = $"(?:{(FlavorType == TimerType.Ascending ? "ascending|asc" : "descending|desc?")})";
            var m = Regex.Match(command,
                $@"^\s*{flavorRegex}\s+timer\s+(?<first>[0-{(char) ('0' + _a - 1)}])\s*(?<last>[0-{(char) ('0' + _b - 1)}])\s*",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (m.Success)
                return ProcessTwitchCommandInternal(m.Groups["first"].Value[0] - '0', m.Groups["last"].Value[0] - '0');

            if (!_running && Regex.IsMatch(command, $@"^\s*{flavorRegex}\s+timer\s+reset\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return ProcessTwitchReset();

            return null;
        }

        public IEnumerator ProcessTwitchReset()
        {
            _prefab.Selectable.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        public IEnumerator ProcessTwitchCommandInternal(int first, int last)
        {
            if (!_running)
            {
                _prefab.Selectable.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            var state = first * _b + last;
            yield return new WaitUntil(() => _displayedTime == state);
            _prefab.Selectable.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            if (State == desiredState)
                yield break;

            if (!_running)
            {
                _prefab.Selectable.OnInteract();
                yield return new WaitForSeconds(.1f);
            }

            while (_displayedTime != desiredState)
                yield return true;
            _prefab.Selectable.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}
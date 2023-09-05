using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Variety
{
    public class Die : Item
    {
        private static readonly Vector3 SLPosition = new Vector3(0.075167f, 0f, 0.076057f);
        private DiePrefab _prefab;
        private readonly int _topLeftCell;
        private int _top, _turn;
        private Quaternion _trueRot = Quaternion.identity;
        private readonly bool _flavor;

        public Die(VarietyModule module, int tlc, bool flavor) : base(module, CellRect(tlc, 2, 2))
        {
            _topLeftCell = tlc;
            SetState(-1, automatic: true);
            _flavor = flavor;
        }

        private static readonly int[][] _turns = new int[][]
        {
                new int[] { 2, 4, 5, 3 },
                new int[] { 5, 4, 2, 3 },
                new int[] { 1, 4, 0, 3 },
                new int[] { 5, 1, 2, 0 },
                new int[] { 5, 0, 2, 1 },
                new int[] { 0, 4, 1, 3 },
        };

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.DieTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(_topLeftCell, 2), .015f, GetYOfCellRect(_topLeftCell, 2));
            _prefab.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            _prefab.transform.localRotation = Quaternion.FromToRotation(new Vector3(1f, 0f, 1f), SLPosition - new Vector3(_prefab.transform.localPosition.x, 0f, _prefab.transform.localPosition.z));

            var c1 = Color.HSVToRGB(Random.value, Random.Range(0.1f, 0.2f), Random.Range(0.8f, 0.9f));
            var c2 = Color.HSVToRGB(Random.value, Random.Range(0.5f, 0.6f), Random.Range(0.1f, 0.2f));
            var rends = _prefab.Model.GetComponentsInChildren<MeshRenderer>();
            if (!_flavor)
            {
                Color tmp = c2;
                c2 = c1;
                c1 = tmp;
            }

            foreach (var rend in rends)
            {
                rend.materials[0].color = c1;
                if (rend.materials.Length >= 2)
                    rend.materials[1].color = c2;
            }

            var rots = Ut.NewArray(
                Quaternion.identity, // 1 5
                Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f)), // 5 0
                Quaternion.AngleAxis(-90f, new Vector3(1f, 0f, 0f)), // 2 1
                Quaternion.AngleAxis(90f, new Vector3(0f, 0f, 1f)), // 4 5
                Quaternion.AngleAxis(-90f, new Vector3(0f, 0f, 1f)), // 3 5
                Quaternion.AngleAxis(180f, new Vector3(1f, 0f, 0f))); // 0 2
            var r = Random.Range(0, 6);
            _turn = Random.Range(0, 4);

            var rot2 = Quaternion.AngleAxis(_turn * 90f, new Vector3(0f, 1f, 0f)); // 1 = cw

            _prefab.Model.transform.localRotation = _trueRot = rot2 * rots[r];
            _top = new int[] { 1, 5, 2, 4, 3, 0 }[r];
            SetState(DigitsToState(_top, _turns[_top][_turn]), automatic: true);

            for (var i = 0; i < 4; i++)
            {
                _prefab.Selectables[i].OnInteract = ArrowPressed(i);
                yield return new ItemSelectable(_prefab.Selectables[i], Cells[0] + (i % 2) + W * (i / 2));
            }
        }

        private int DigitsToState(int up, int sl)
        {
            switch ($"{up}{sl}")
            {
                case "02": return 0;
                case "03": return 6;
                case "04": return 12;
                case "05": return 18;
                case "12": return 1;
                case "13": return 7;
                case "14": return 13;
                case "15": return 19;
                case "20": return 2;
                case "21": return 8;
                case "23": return 14;
                case "24": return 20;
                case "30": return 3;
                case "31": return 9;
                case "32": return 15;
                case "35": return 21;
                case "40": return 4;
                case "41": return 10;
                case "42": return 16;
                case "45": return 22;
                case "50": return 5;
                case "51": return 11;
                case "53": return 17;
                case "54": return 23;
            }

            Debug.LogError($"<Variety #{Module.ModuleID}> Bad dice state: {up}{sl}");
            return -1;
        }

        private KMSelectable.OnInteractHandler ArrowPressed(int arrowIx) => delegate
        {
            Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _prefab.Selectables[arrowIx].transform);
            switch (arrowIx)
            {
                case 0:
                    _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(0f, 0f, -90f) * _trueRot));
                    int p = _top;
                    _top = _turns[_top][(_turn + 3) % 4];
                    _turn = Array.IndexOf(_turns[_top], _turns[p][_turn]);
                    break;
                case 1:
                    _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(-90f, 0f, 0f) * _trueRot));
                    int t = _top;
                    _top = _turns[_top][(_turn + 2) % 4];
                    _turn = Array.IndexOf(_turns[_top], t);
                    break;
                case 2:
                    _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(90f, 0f, 0f) * _trueRot));
                    int q = _top;
                    _top = _turns[_top][_turn];
                    _turn = Array.IndexOf(_turns[_top], Flip(q));
                    break;
                case 3:
                    _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(0f, 0f, 90f) * _trueRot));
                    int top = _top;
                    _top = _turns[_top][(_turn + 1) % 4];
                    _turn = Array.IndexOf(_turns[_top], _turns[top][_turn]);
                    break;
            }

            SetState(DigitsToState(_top, _turns[_top][_turn]));
            return false;
        };

        private int Flip(int num) => (7 - num) % 6;

        private IEnumerator Animate(Transform tr, Quaternion end)
        {
            float startTime = Time.time;
            Quaternion start = tr.localRotation;
            while (Time.time - startTime < 0.25f)
            {
                tr.localRotation = Quaternion.Slerp(start, end, (Time.time - startTime) * 4f);
                tr.localPosition = new Vector3(0f, (0.125f - Mathf.Abs(Time.time - startTime - 0.125f)) * 0.1f + 0.009f);
                yield return null;
            }
            tr.localRotation = end;
            tr.localPosition = new Vector3(0f, 0.009f);
        }

        public override int NumStates => 24;
        public override object Flavor => _flavor ? DieFlavor.DarkOnLight : DieFlavor.LightOnDark;

        public override string ToString() => $"{(_flavor ? "dark-on-light" : "light-on-dark")} die";
        public override string TwitchHelpMessage => "!{0} light-on-dark die 1234 [press the rotation buttons; buttons are numbered clockwise from the one pointing towards the status light]";

        public override string DescribeSolutionState(int state)
        {
            var top = state % 6;
            var sl = new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != top && i != Flip(top)).ElementAt(state / 6);
            return $"rotate the {(_flavor ? "dark-on-light" : "light-on-dark")} die so you can see the {top} side and the {sl} side is facing the status light";
        }

        public override string DescribeWhatUserDid() => $"you rotated the {(_flavor ? "dark-on-light" : "light-on-dark")} die";

        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            int top = desiredState % 6;
            int sl = new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != top && i != Flip(top)).ToArray()[desiredState / 6];
            return $"you should have rotated the {(_flavor ? "dark-on-light" : "light-on-dark")} die so you can see the {top} side and the {sl} side is facing the status light (you can see the {_top} side and the {_turns[_top][_turn]} side is facing the status light)";
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, $@"^\s*{(_flavor ? @"(?:dark-?on-?light|do?l)" : @"(?:light-?on-?dark|lo?d)")}\s+die\s+([1-4]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.ECMAScript);
            return m.Success ? TwitchPress(m.Groups[1].Value) : null;
        }

        private IEnumerator TwitchPress(string b)
        {
            var shuffle = new int[] { 1, 3, 2, 0 };
            foreach (var sel in b.Select(c => _prefab.Selectables[shuffle[c - '1']]))
            {
                sel.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            if (State == desiredState)
                return Enumerable.Empty<object>();

            var visited = new Dictionary<int, int>();
            var dirs = new Dictionary<int, int>();
            var q = new Queue<int>();
            q.Enqueue(State);

            while (q.Count > 0)
            {
                var item = q.Dequeue();
                var adjs = new List<int>();

                var stop = item % 6;
                var sturn = Array.IndexOf(_turns[stop], new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != stop && i != Flip(stop)).ToArray()[item / 6]);

                var top = _turns[stop][(sturn + 3) % 4];
                var turn = Array.IndexOf(_turns[top], _turns[stop][sturn]);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][(sturn + 2) % 4];
                turn = Array.IndexOf(_turns[top], stop);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][sturn];
                turn = Array.IndexOf(_turns[top], Flip(stop));
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][(sturn + 1) % 4];
                turn = Array.IndexOf(_turns[top], _turns[stop][sturn]);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                for (var i = 0; i < adjs.Count; i++)
                {
                    var adj = adjs[i];
                    if (adj != State && !visited.ContainsKey(adj))
                    {
                        visited[adj] = item;
                        dirs[adj] = i;
                        if (adj == desiredState)
                            goto done;
                        q.Enqueue(adj);
                    }
                }
            }

            done:
            var moves = new List<int>();
            var curPos = desiredState;
            var iter = 0;
            while (curPos != State)
            {
                iter++;
                if (iter > 100)
                {
                    Debug.LogFormat("<> State = {0}", State);
                    Debug.LogFormat("<> desiredState = {0}", desiredState);
                    Debug.LogFormat("<> moves = {0}", moves.Join(","));
                    Debug.LogFormat("<> visited:\n{0}", visited.Select(kvp => $"{kvp.Key} <= {kvp.Value}").Join("\n"));
                    throw new InvalidOperationException();
                }

                moves.Add(dirs[curPos]);
                curPos = visited[curPos];
            }

            moves.Reverse();
            return TwitchMove(moves);
        }

        private IEnumerable<object> TwitchMove(List<int> moves)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                _prefab.Selectables[moves[i]].OnInteract();
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}
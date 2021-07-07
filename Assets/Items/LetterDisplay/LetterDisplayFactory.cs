using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

namespace Variety
{
    public class LetterDisplayFactory : ItemFactory
    {
        public static readonly string[] _words = "ACE,ACT,AID,AIM,AIR,AND,ANT,APT,ARM,ART,AYE,BAD,BAG,BAN,BAR,BAT,BAY,BED,BEE,BEG,BET,BID,BIG,BIT,BOB,BOW,BOY,BUD,BUT,BUY,BYE,CAB,CAN,CAP,CAR,CAT,COP,COT,COW,CUE,CUP,CUT,DAD,DAM,DAY,DEN,DIE,DIG,DIM,DIP,DOG,DON,DOT,DRY,DUE,DUG,DYE,EAR,EAT,FAN,FAR,FAT,FAX,FED,FEE,FEN,FEW,FIN,FIT,FIX,FLY,FOG,FOR,FRY,FUN,FUR,GET,GIG,GIN,GUM,GUN,GUT,GUY,HAM,HAT,HAY,HEN,HER,HEY,HIM,HIP,HIT,HOP,HOT,HOW,HUT,JAM,JAR,JAW,JOB,JOY,KID,KIN,KIT,LAB,LAD,LAP,LAW,LAY,LEG,LET,LID,LIE,LIP,LIT,LOG,LOO,LOT,LOW,MAD,MAN,MAP,MAT,MAX,MAY,MIC,MID,MIX,MOB,MOD,MUD,MUG,MUM,NET,NEW,NOD,NOR,NOT,NOW,NUN,NUT,OPT,OUR,OUT,PAD,PAN,PAR,PAT,PAY,PEG,PEN,PER,PET,PIE,PIG,PIN,PIT,POP,POT,POW,PUB,PUT,RAG,RAM,RAT,RAW,RED,RIB,RID,RIG,RIM,ROB,ROD,ROT,ROW,RUB,RUG,RUM,RUN,SAD,SAW,SAY,SEA,SEE,SET,SHE,SHY,SIC,SIG,SIN,SIR,SIT,SIX,SLY,SND,SON,SUE,SUM,SUN,TAG,TAP,TAX,TEA,TEE,TEN,THE,THY,TIE,TIN,TIP,TOE,TOO,TOP,TOY,TRN,TRY,VAT,VET,WAR,WAX,WAY,WEE,WET,WHY,WIG,WIN,WIT,WRY,YEN,YET".Split(',');

        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            if (taken.Contains(this))
                return null;

            var availableLocations = Enumerable.Range(0, W * H).Where(cell => isRectAvailable(taken, cell, 4, 3)).ToArray();
            if (availableLocations.Length == 0)
                return null;

            var location = availableLocations[Rnd.Range(0, availableLocations.Length)];
            claimRect(taken, location, 4, 3);
            taken.Add(this);

            var letters = new char[3][];
            var guaranteedWord1Ix = Rnd.Range(0, _words.Length);
            var guaranteedWord2Ix = Rnd.Range(0, _words.Length - 1);
            if (guaranteedWord2Ix >= guaranteedWord1Ix)
                guaranteedWord2Ix++;
            for (var i = 0; i < 3; i++)
            {
                var ltrs = new List<char> { _words[guaranteedWord1Ix][i] };
                if (_words[guaranteedWord2Ix][i] != _words[guaranteedWord1Ix][i])
                    ltrs.Add(_words[guaranteedWord2Ix][i]);
                while (ltrs.Count < 3)
                {
                    var additionalLetter = (char) ('A' + Rnd.Range(0, 26));
                    if (!ltrs.Contains(additionalLetter))
                        ltrs.Add(additionalLetter);
                }
                ltrs.Shuffle();
                letters[i] = ltrs.ToArray();
            }

            var formableWords = (
                from ltr1 in letters[0]
                from ltr2 in letters[1]
                from ltr3 in letters[2]
                select string.Concat(ltr1, ltr2, ltr3)).ToArray();
            var formableValidWords = formableWords.Where(w => _words.Contains(w)).ToArray();
            Array.Sort(formableWords, StringComparer.InvariantCultureIgnoreCase);
            return new LetterDisplay(module, location, letters, formableValidWords);
        }

        public override IEnumerable<object> Flavors { get { yield return "Letter Display"; } }
    }
}

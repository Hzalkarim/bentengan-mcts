using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Bentengan
{
    public class BattleEvaluator : Node2D
    {
        public static BattleEvaluator Instance { get; private set;}

        public override void _Ready()
        {
            if (Instance == null)
                Instance = this;
        }

        //out the clipped cell for one cluster surrounding
        public int[] GetInvalidMovement(int center, int arenaLength, int arenaHeight)
        {
            int cellCount = arenaHeight * arenaLength;
            if (!CheckCellWithinBound(center, arenaLength, arenaHeight))
            {
                return new int[1];
            }
            
            List<int> invalidDir = new List<int>(12);
            if (center < arenaLength)
            {
                invalidDir.Add(-arenaLength);
                invalidDir.Add(-arenaLength - 1);
                invalidDir.Add(-arenaLength + 1);
            }

            if (center >= cellCount - arenaLength)
            {
                invalidDir.Add(arenaLength);
                invalidDir.Add(arenaLength - 1);
                invalidDir.Add(arenaLength + 1);
            }

            if (center % arenaLength == 0)
            {
                invalidDir.Add(-arenaLength - 1);
                invalidDir.Add(-1);
                invalidDir.Add(arenaLength - 1);
            }

            if (center % arenaLength == arenaLength - 1)
            {
                invalidDir.Add(-arenaLength + 1);
                invalidDir.Add(1);
                invalidDir.Add(arenaLength + 1);
            }

            return invalidDir.Distinct().ToArray();
        }

        public bool CheckCellWithinBound(int center, int arenaLength, int arenaHeight)
        {
            int cellCount = arenaHeight * arenaLength;
            return center >= 0 && center < cellCount;
        }

        public bool CheckCastleCapture(int[] castleArea, int[] capturerPos)
        {
            return castleArea.Intersect(capturerPos).Count() > 0;
        }

        public bool CheckTeamEliminated(PersonPieceData[] data)
        {
            return data.All(p => p.isCaptured);
        }

        public bool CheckTeammateRescue(int rescuee, int rescuer, int[] rescuerCaptureArea, Action<int> onRescue)
        {
            if (rescuerCaptureArea.Contains(rescuee - rescuer))
            {
                onRescue?.Invoke(rescuee);
                return true;
            }
            return false;
        }

        public bool CheckPersonPieceCapture(int aPerson, int aLiveTime, int[] aPersonCaptureArea,
            int bPerson, int bLiveTime, int[] bPersonCaptureArea, Action<int> onCapturing)
        {
            bool aIsCapturing = aLiveTime < bLiveTime;
            int capturer = aIsCapturing ? aPerson : bPerson;
            int captured = aIsCapturing ? bPerson : aPerson;
            int[] capturerArea = aIsCapturing ? aPersonCaptureArea : bPersonCaptureArea;

            foreach (int area in capturerArea)
            {
                if (area == captured - capturer)
                {
                    if (aLiveTime == bLiveTime)
                    {
                        onCapturing?.Invoke(captured);
                        onCapturing?.Invoke(capturer);
                        return true;
                    }
                    onCapturing?.Invoke(captured);
                    return true;
                }
            }

            return false;
        }

        public bool CheckPersonPieceCapture(PersonPieceData personA, PersonPieceData personB, Action<int> onCapturing)
        {
            if (personA.isCaptured || personB.isCaptured) 
                return false;

            return CheckPersonPieceCapture(
                personA.cellPosition, personA.liveTime,
                personA.CaptureArea,
                personB.cellPosition, personB.liveTime,
                personB.CaptureArea, onCapturing);
        }

        public int GetEmptyJail(int[] jailArea, int[] allPerson)
        {
            if (jailArea.Length == 0) return -1;
            return jailArea.First(i => !allPerson.Contains(i));
        }

        public bool CheckIsInOpponentJail(int pos, int[] opponentJailArea)
        {
            return opponentJailArea.Contains(pos);
        }

        public int GetEmptyCastle(int[] castleArea, int[] teamPersons)
        {
            if (castleArea.Length == 0) return -1;
            return castleArea.First(i => !teamPersons.Contains(i));
        }

        public bool CheckIsInOwnCastle(int pos, int[] teamCastleArea)
        {
            return teamCastleArea.Contains(pos);
        }
    }

}
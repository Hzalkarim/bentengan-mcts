using System;
using System.Linq;

namespace Bentengan.Utility
{
    public static class StrategyAlgorithmCalculator
    {
        private static ArenaData _arenaData;

        public static void SetArenaData(ArenaData arena)
        {
            _arenaData = arena;
        }


        public static int CaptureOpponentCastle(this int personCellPos, string teamName)
        {
            int[] opponentCastle = _arenaData.teamDatas.First(t => !t.teamName.Equals(teamName)).castleArea;

            return ApproachNearestTarget(opponentCastle, personCellPos);
        }

        public static int BackToOwnCastle(this int personCellPos, string teamName)
        {
            int[] ownCastle = _arenaData.teamDatas.First(t => t.teamName.Equals(teamName)).castleArea;

            return ApproachNearestTarget(ownCastle, personCellPos);

        }

        public static int CaptureOpponentPerson(this int personCellPos, string teamName)
        {
            int[] opponentTeamPerson = _arenaData.personPieceDatas
                .Where(p => !p.teamName.Equals(teamName) && !p.isCaptured).Select(q => q.cellPosition).ToArray();
            return ApproachNearestTarget(opponentTeamPerson, personCellPos);
        }

        #region LOW PRIORITY
        public static int Standby(this int personCellPos, string teamName)
        {
            return personCellPos;
        }

        public static int RescueTeamPerson(this int personCellPos, string teamName)
        {
            int[] friendlyTeamPerson = _arenaData.personPieceDatas
                .Where(p => p.teamName.Equals(teamName) && p.isCaptured).Select(q => q.cellPosition).ToArray();
            return ApproachNearestTarget(friendlyTeamPerson, personCellPos);
        }
        #endregion

        public static int CalculateStrategy(this int personCellPos, MctsStrategy strategy, string teamName)
        {
            switch (strategy)
            {
                case MctsStrategy.CaptureCastle:
                    return personCellPos.CaptureOpponentCastle(teamName);
                case MctsStrategy.BackToCastle:
                    return personCellPos.BackToOwnCastle(teamName);
                case MctsStrategy.CaptureOpponent:
                    return personCellPos.CaptureOpponentPerson(teamName);
                case MctsStrategy.RescueTeam:
                    return personCellPos.RescueTeamPerson(teamName);
                case MctsStrategy.Standby:
                    return personCellPos.Standby(teamName);
                default:
                    return -1;
            }
        }

        private static int ApproachNearestTarget(int[] targetGroup, int cellPos)
        {
            double d = double.MaxValue;
            int idx = -1;
            for (int i = 0; i < targetGroup.Length; i++)
            {
                double sqrDist = CellSquareDistance(targetGroup[i], cellPos);
                if (sqrDist < d)
                {
                    d = sqrDist;
                    idx = i;
                }
            }

            return idx == -1 ? 0 : ApproachTarget(targetGroup[idx], cellPos);
        }

        private static double CellSquareDistance(int a, int b)
        {
            int xDist = a % _arenaData.length - b % _arenaData.length;
            int yDist = (int)(a / _arenaData.length) - (int)(b / _arenaData.length);

            return Math.Pow(xDist, 2) + Math.Pow(yDist, 2);
        }

        private static int ApproachTarget(int target, int source)
        {
            int xDist = target % _arenaData.length - source % _arenaData.length;
            int yDist = (int)(target / _arenaData.length) - (int)(source / _arenaData.length);

            int move = 0;
            if (xDist > 0)
                move++;
            else if (xDist < 0)
                move--;

            if (yDist > 0)
                move += _arenaData.length;
            else if (yDist < 0)
                move -= _arenaData.length;

            return move + source;
        }
    }

    public enum MctsStrategy
    {
        CaptureCastle, BackToCastle, CaptureOpponent, RescueTeam, Standby
    }
}
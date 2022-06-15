using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Bentengan
{
    public class BattleFlowManager : Node2D
    {
        public event Action<string, GameplayHighlight> gameplayHighlightEvent;

        private BattleEvaluator _evaluator;
        private List<PersonPieceMovement> _registeredMove = new List<PersonPieceMovement>();
        private List<PersonPieceMovement> _systematicMove = new List<PersonPieceMovement>();

        public BattleEvaluator Evaluator => _evaluator;
        public List<PersonPieceMovement> RegisteredMove => _registeredMove;

        public override void _Ready()
        {
            _evaluator = GetNode<BattleEvaluator>("../BattleEvaluator");
        }

        public int FindOwnEmptyCastle(PersonPieceData rescuee, ArenaData arena)
        {
            bool isTeammate = rescuee.teamName.Equals(arena.teamDatas[0].teamName);
            int ownTeamIndex = isTeammate ? 0 : 1;

            int emptyCastle = Evaluator.GetEmptyCastle(arena.teamDatas[ownTeamIndex].castleArea,
                arena.personPieceDatas.Select(i => i.cellPosition).ToArray());

            return emptyCastle;
        }

        public void CastleCaptured(ArenaData arena)
        {
            bool firstTeamWin = Evaluator.CheckCastleCapture(
                arena.teamDatas[1].castleArea,
                arena.personPieceDatas.Where(p => p.teamName.Equals(arena.teamDatas[0].teamName))
                    .Select(i => i.cellPosition).ToArray());

            bool secondTeamWin = Evaluator.CheckCastleCapture(
                arena.teamDatas[0].castleArea,
                arena.personPieceDatas.Where(p => p.teamName.Equals(arena.teamDatas[1].teamName))
                    .Select(i => i.cellPosition).ToArray());

            if (firstTeamWin && secondTeamWin)
            {
                gameplayHighlightEvent?.Invoke("Game", GameplayHighlight.GameDraw);
            }
            else if (firstTeamWin)
            {
                gameplayHighlightEvent?.Invoke(arena.teamDatas[0].teamName, GameplayHighlight.GameWon);
            }
            else if (secondTeamWin)
            {
                gameplayHighlightEvent?.Invoke(arena.teamDatas[1].teamName, GameplayHighlight.GameWon);
            }
        }

        public void AllPersonCaptured(PersonPieceData[] firstTeam, PersonPieceData[] secondTeam)
        {
            bool firstTeamWin = secondTeam.All(p => p.isCaptured);
            bool secondTeamWin = firstTeam.All(p => p.isCaptured);

            if (firstTeamWin && secondTeamWin)
            {
                gameplayHighlightEvent?.Invoke("Game", GameplayHighlight.GameDraw);
            }
            else if (firstTeamWin)
            {
                gameplayHighlightEvent?.Invoke(firstTeam[0].teamName, GameplayHighlight.GameWon);
            }
            else if (secondTeamWin)
            {
                gameplayHighlightEvent?.Invoke(secondTeam[0].teamName, GameplayHighlight.GameWon);
            }
        }

        public void RegisterRescueeToCastle(PersonPieceData rescuee, ArenaData arena)
        {
            int emptyCastle = FindOwnEmptyCastle(rescuee, arena);
            //GD.Print($"{rescuee.cellPosition} rescued");
            if (emptyCastle == -1) return;
            _systematicMove.Add(new PersonPieceMovement(rescuee.teamName, rescuee.cellPosition, emptyCastle));
        }

        public void SendAllRescueeToCastle(PersonPieceData[] personsTeamA, PersonPieceData[] personsTeamB, ArenaData arena)
        {
            PersonPieceData[][] teams = new PersonPieceData[][] { personsTeamA, personsTeamB};
            for (int i = 0; i < teams.Length; i++)
            {
                foreach (PersonPieceData rescuer in teams[i])
                {
                    if (rescuer.isCaptured) continue;
                    var rescuees = teams[i].Where(p => p.cellPosition != rescuer.cellPosition);
                    foreach (PersonPieceData rescuee in rescuees)
                    {
                        if (!rescuee.isCaptured) continue;
                        bool isRescue = Evaluator.CheckTeammateRescue(rescuee.cellPosition, rescuer.cellPosition, rescuer.CaptureArea,
                            (idx) => RegisterRescueeToCastle(rescuee, arena));

                        if (isRescue)
                            gameplayHighlightEvent?.Invoke(rescuee.teamName, GameplayHighlight.PersonRescued);
                    }
                }
            }
        }

        //use to find empty jail for the captured
        public int FindOpponentEmptyJail(PersonPieceData captured, ArenaData arena)
        {
            bool isTeammate = captured.teamName.Equals(arena.teamDatas[0].teamName);
            int opposingTeamIndex = isTeammate ? 1 : 0;

            int emptyJail = Evaluator.GetEmptyJail(
                arena.teamDatas[opposingTeamIndex].jailArea,
                arena.personPieceDatas.Select(i => i.cellPosition).ToArray());

            return emptyJail;
        }

        public void RegisterCapturedToJail(PersonPieceData captured, ArenaData arena)
        {
            int emptyJail = FindOpponentEmptyJail(captured, arena);
            //GD.Print($"{captured.cellPosition} captured");
            if (emptyJail == -1)
                return;
            _systematicMove.Add(new PersonPieceMovement(captured.teamName, captured.cellPosition, emptyJail));
        }

        public void SendAllCapturedToJail(PersonPieceData[] personsTeamA, PersonPieceData[] personsTeamB, ArenaData arena)
        {
            foreach (PersonPieceData personA in personsTeamA)
            {
                foreach (PersonPieceData personB in personsTeamB)
                {
                    bool isCaptured = Evaluator.CheckPersonPieceCapture(personA, personB,
                        (int i) =>
                        {
                            if (i == personA.cellPosition && i == personB.cellPosition)
                            {
                                if (personA.liveTime == personB.liveTime)
                                {
                                    SendToJail(personA, arena);
                                    SendToJail(personB, arena);
                                }
                                else if (personA.liveTime > personB.liveTime)
                                {
                                    SendToJail(personA, arena);
                                }
                                else
                                {
                                    SendToJail(personB, arena);
                                }
                            }
                            else if (i == personA.cellPosition)
                            {
                                SendToJail(personA, arena);
                            }                            
                            else if (i == personB.cellPosition)
                            {
                                SendToJail(personB, arena);
                            }
                        });
                }
            }
        }

        private void SendToJail(PersonPieceData p, ArenaData arena)
        {
            RegisterCapturedToJail(p, arena);
            gameplayHighlightEvent?.Invoke(p.teamName, GameplayHighlight.PersonCaptured);
        }

        public void UpdateAllPersonPieceInvalidMovement(int pos, int arenaLength, int arenaHeight, Action<int[]> onUpdate)
        {
            onUpdate?.Invoke(Evaluator.GetInvalidMovement(pos, arenaLength, arenaHeight));
        }

        public void ExecuteRegisteredMove(Action<int, int> onMove)
        {
            _registeredMove.ForEach(i => onMove(i.from, i.to));
            _registeredMove.Clear();
        }

        public void ExecuteSystematicMove(Action<int, int> onMove)
        {
            _systematicMove.ForEach(i => onMove(i.from, i.to));
            _systematicMove.Clear();
        }

        public void RegisterMove(string teamName, int from, int to)
        {
            _registeredMove.Add(new PersonPieceMovement(teamName, from, to));
        }

        public void UnregisterMove(int from)
        {
            for (int i = 0; i < _registeredMove.Count; i++)
            {
                if (_registeredMove[i].from == from)
                {
                    _registeredMove.RemoveAt(i);
                    return;
                }
            }
        }

        public bool IsMoveRegistered(int from)
        {
            return _registeredMove.Any(i => i.from == from);
        }

    }

    public struct PersonPieceMovement
    {
        public string teamName;
        public int from;
        public int to;

        public PersonPieceMovement(string teamName, int from, int to)
        {
            this.teamName = teamName;
            this.from = from;
            this.to = to;
        }
    }

    public enum GameplayHighlight
    {
        GameWon, GameDraw, PersonCaptured, PersonRescued
    }

}
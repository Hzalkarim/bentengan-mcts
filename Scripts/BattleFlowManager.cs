using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Bentengan
{
    public class BattleFlowManager : Node2D
    {
        private BattleEvaluator _evaluator;
        private List<PersonPieceMovement> _registeredMove = new List<PersonPieceMovement>();
        private List<PersonPieceMovement> _systematicMove = new List<PersonPieceMovement>();

        public BattleEvaluator Evaluator => _evaluator;

        public override void _Ready()
        {
            _evaluator = GetNode<BattleEvaluator>("../../BattleEvaluator");
        }

        public int FindOwnEmptyCastle(PersonPieceData rescuee, ArenaData arena)
        {
            bool isTeammate = rescuee.teamName.Equals(arena.teamDatas[0].teamName);
            int ownTeamIndex = isTeammate ? 0 : 1;

            int emptyCastle = Evaluator.GetEmptyCastle(arena.teamDatas[ownTeamIndex].castleArea,
                arena.personPieceDatas.Select(i => i.cellPosition).ToArray());

            return emptyCastle;
        }

        public void SendRescueeToCastle(PersonPieceData captured, ArenaData arena)
        {
            int emptyCastle = FindOwnEmptyCastle(captured, arena);
            GD.Print($"{captured.cellPosition} rescued");
            _systematicMove.Add(new PersonPieceMovement(captured.cellPosition, emptyCastle));
        }

        public void SendAllRescueeToCastle(PersonPieceData[] personsTeamA, PersonPieceData[] personsTeamB, ArenaData arena)
        {
            PersonPieceData[][] teams = new PersonPieceData[][] { personsTeamA, personsTeamB};
            for (int i = 0; i < teams.Length; i++)
            {
                foreach (PersonPieceData rescuer in teams[i])
                {
                    var rescuees = teams[i].Where(p => p.cellPosition != rescuer.cellPosition);
                    foreach (PersonPieceData rescuee in rescuees)
                    {
                        if (!rescuee.isCaptured) continue;
                        Evaluator.CheckTeammateRescue(rescuee.cellPosition, rescuer.cellPosition, rescuer.CaptureArea,
                            (idx) => SendRescueeToCastle(rescuee, arena));
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

        public void SendCapturedToJail(PersonPieceData captured, ArenaData arena)
        {
            int emptyJail = FindOpponentEmptyJail(captured, arena);

            _systematicMove.Add(new PersonPieceMovement(captured.cellPosition, emptyJail));
        }

        public void SendAllCapturedToJail(PersonPieceData[] personsTeamA, PersonPieceData[] personsTeamB, ArenaData arena)
        {
            foreach (PersonPieceData personA in personsTeamA)
            {
                foreach (PersonPieceData personB in personsTeamB)
                {
                    Evaluator.CheckPersonPieceCapture(personA, personB,
                        (int i) =>
                        {
                            if (i == personA.cellPosition)
                                SendCapturedToJail(personA, arena);
                            else if (i == personB.cellPosition)
                                SendCapturedToJail(personB, arena);
                        });
                }
            }
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

        public void RegisterMove(int from, int to)
        {
            _registeredMove.Add(new PersonPieceMovement(from, to));
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

    struct PersonPieceMovement
    {
        public int from;
        public int to;

        public PersonPieceMovement(int from, int to)
        {
            this.from = from;
            this.to = to;
        }
    }

}
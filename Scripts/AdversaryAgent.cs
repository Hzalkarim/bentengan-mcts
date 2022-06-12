using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Bentengan.Mcts;

namespace Bentengan
{
    public class AdversaryAgent : Node
    {
        private Arena _arena;
        private Team _team;

        private MctsNode _pickedNode;

        [Export]
        private string _teamName;
        private string _opponentTeamName;
        [Export]
        private bool isActive;
        [Export]
        private string _mctsName;
        private MonteCarloTreeSearch _mcts;
        [Export]
        private string _mctsOpponentName;
        private MonteCarloTreeSearch _mctsOpponent;

        public MonteCarloTreeSearch Mcts => _mcts;

        public override void _Ready()
        {
            if (!isActive) return;
            _arena = GetNode<Arena>("../../Arena");
            _team = GetNode<Team>($"../../TeamPositionings/{_arena.TeamPositionings}/{_teamName}");
            _opponentTeamName = _arena.Teams.First(n => !n.TeamName.Equals(_teamName)).TeamName;

            _mcts = GetNode<MonteCarloTreeSearch>($"{_mctsName}/MCTS");
            _mcts.RegisterToSimulatedArenaEvents();
            _mctsOpponent = GetNode<MonteCarloTreeSearch>($"{_mctsOpponentName}/MCTS");
            _mctsOpponent.RegisterToSimulatedArenaEvents();

            _mcts.onFinishGenerateTreeEvent += OnTreeFinished;
            _mctsOpponent.onFinishGenerateTreeEvent += OnTreeFinished;
            _mcts.onBackpropagationEndEvent += OnOwnBackpropagationEnd;
            _mctsOpponent.onBackpropagationEndEvent += OnOwnBackpropagationEnd;
        }

        public void RegisterBestMove()
        {
            _mcts.StopSimulation();
            _pickedNode = _mcts.GetMaxAvgChildNode(_mcts.Root);
            if (_pickedNode == null) return;
            int[] movement = _arena.ToData().personPieceDatas
                .Where(p => p.teamName.Equals(_teamName)).Select(q => q.cellPosition).ToArray();
            GD.Print(_pickedNode.ToString());
            for (int i = 0; i < movement.Length; i++)
            {
                _arena.TryRegisterMove(_teamName, movement[i], _pickedNode.registeredMove[i]);
            }
        }

        public void GenerateTree()
        {
            if (_team.PersonPieces.All(p => p.IsCaptured))
            {
                GD.Print("Not generate tree: all person captured");
                return;
            }

            _mcts.Root = new MctsNode();
            _mctsOpponent.Root = new MctsNode();

            _mcts.GenerateTreeFromRoot();
            _mctsOpponent.GenerateTreeFromRoot();
        }

        public void RegisterRandomMove()
        {
            var tos = new List<int>(3);
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            _team.PersonPieces.ForEach(p => 
            {
                var mv = p.MovementArea;
                if (mv.Length == 0) return;

                int to = mv[rnd.RandiRange(0, mv.Length - 1)] + p.CellPosition;
                if (tos.Contains(to)) return;

                tos.Add(to);
                _arena.RegisterMove(p.TeamName, p.CellPosition, to);
            });
        }

        private void OnOwnBackpropagationEnd(MonteCarloTreeSearch tree)
        {
            //GD.Print($"backpropagation, isownturn {isOwnTurn}");
            if (tree == _mcts)
            {
                if (_mcts.Root.timesVisit < _mcts.LimitVisit)
                {
                    _mcts.GenerateTreeFromRoot(_mctsOpponent.Root);
                }
            }
            else if (tree == _mctsOpponent)
            {
                if (_mctsOpponent.Root.timesVisit < _mctsOpponent.LimitVisit)
                {
                    _mctsOpponent.GenerateTreeFromRoot(_mcts.Root);
                }
            }
        }

        private void RegisterBestMoveOnSelection(string teamName, int[] regMove)
        {
            var team = _mcts.SimArena.ArenaData.personPieceDatas.Where(p => p.teamName.Equals(teamName)).ToArray();
            for (int i = 0; i < regMove.Length; i++)
            {
                _mcts.SimArena.TryRegisterMove(teamName, team[i].cellPosition, regMove[i]);
            }
        }

        private void OnTreeFinished()
        {
            GD.Print("Stopped Selection");
        }
    }

}
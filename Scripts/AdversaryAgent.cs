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

        private MonteCarloTreeSearch _mcts;
        private MonteCarloTreeSearch _mctsForPlayer;
        private MctsNode _pickedNode;

        [Export]
        private string _teamName;
        [Export]
        private bool isActive;

        public MonteCarloTreeSearch Mcts => _mcts;

        public override void _Ready()
        {
            if (!isActive) return;
            _arena = GetNode<Arena>("../../Arena");
            _team = GetNode<Team>($"../../TeamPositionings/{_arena.TeamPositionings}/{_teamName}");

            _mcts = GetNode<MonteCarloTreeSearch>("MCTS");
            _mcts.RegisterToSimulatedArenaEvents();
            _mctsForPlayer = GetNode<MonteCarloTreeSearch>("MCTS_ForPlayer");

            _mcts.onFinishGenerateTreeEvent += OnTreeFinished;
        }

        public void RegisterBestMove()
        {
            _mcts.StopSimulation();
            _pickedNode = _mcts.GetMaxAvgChildNode(_mcts.Root);
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

            if (_pickedNode == null)
            {
                _pickedNode = new MctsNode();
            }
            else
            {
                _pickedNode.parent = null;
                _mcts.LimitVisit += _pickedNode.timesVisit;
            }
            _mcts.Root = _pickedNode;

            _mcts.GenerateTreeFromRoot();
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

        private void OnTreeFinished()
        {
            GD.Print("Stopped Selection");
        }
    }

}
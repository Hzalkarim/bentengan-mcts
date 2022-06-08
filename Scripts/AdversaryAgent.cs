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
        private MctsNode _pickedNode;

        [Export]
        private string _teamName;
        [Export]
        private bool isActive;

        public override void _Ready()
        {
            if (!isActive) return;
            _arena = GetNode<Arena>("../../Arena");
            _team = GetNode<Team>($"../../TeamPositionings/{_arena.TeamPositionings}/{_teamName}");
            _mcts = GetNode<MonteCarloTreeSearch>("MCTS");
        }

        public void RegisterBestMove()
        {
            _mcts.StopSimulation();
            _pickedNode = _mcts.GetMaxScoreChildNode(_mcts.Root);
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
            _mcts.Root = new MctsNode();

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
    }

}
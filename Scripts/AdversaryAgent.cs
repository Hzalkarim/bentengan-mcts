using Godot;
using System;
using System.Collections.Generic;

namespace Bentengan
{
    public class AdversaryAgent : Node
    {
        private Arena _arena;
        private Team _team;

        [Export]
        private string _teamName;

        public override void _Ready()
        {
            _arena = GetNode<Arena>("../../Arena");
            _team = GetNode<Team>($"../../TeamPositionings/{_arena.TeamPositionings}/{_teamName}");
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
                _arena.RegisterMove(p.CellPosition, to);
            });
        }
    }

}
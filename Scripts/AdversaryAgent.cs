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
        [Export]
        private string _teamNodeName;
        private string _opponentTeamName;
        [Export]
        private bool isActive;
        [Export]
        private string _mctsNodeName;
        private MonteCarloTreeSearch _mcts;
        [Export]
        private bool _hasOpponent;
        [Export]
        private string _mctsOpponentNodeName;
        private MonteCarloTreeSearch _mctsOpponent;

        public MonteCarloTreeSearch Mcts => _mcts;

        public override void _Ready()
        {
            Init(_hasOpponent);        
        }

        private void Init(bool hasOpponent)
        {
            if (!isActive) return;
            _arena = GetNode<Arena>("../../Arena");
            _team = GetNode<Team>($"../../TeamPositionings/{_arena.TeamPositionings}/{_teamNodeName}");

            _mcts = GetNode<MonteCarloTreeSearch>($"{_mctsNodeName}/MCTS");
            _mcts.RegisterToSimulatedArenaEvents();

            _mcts.onFinishGenerateTreeEvent += OnTreeFinished;
            _mcts.onBackpropagationEndEvent += OnOwnBackpropagationEnd;

            if (hasOpponent)
                InitOpponent();
        }

        private void InitOpponent()
        {
            _opponentTeamName = _arena.Teams.First(n => !n.TeamName.Equals(_teamName)).TeamName;
            _mctsOpponent = GetNode<MonteCarloTreeSearch>($"{_mctsOpponentNodeName}/MCTS");
            _mctsOpponent.RegisterToSimulatedArenaEvents();
            _mctsOpponent.onFinishGenerateTreeEvent += OnTreeFinished;
            _mctsOpponent.onBackpropagationEndEvent += OnOwnBackpropagationEnd;
        }

        public void RegisterBestMove()
        {
            _mcts.StopSimulation();
            _pickedNode = _mcts.GetMaxAvgChildNode(_mcts.Root);
            if (_pickedNode == null) return;
            int[] movement = _arena.ToData().personPieceDatas
                .Where(p => p.teamName.Equals(_teamName)).Select(q => q.cellPosition).ToArray();

            var childs = _mcts.Root.childs.Select(c => c.ToString()).Take(5);
            var allChild = string.Join(", ", childs);
            GD.Print($"Root child count: {_mcts.Root.childs.Count}, all:{allChild}, best: {_pickedNode.ToString()}" );
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
            _mcts.GenerateTreeFromRoot();
            if (_hasOpponent)
            {
                _mctsOpponent.Root = new MctsNode();
                _mctsOpponent.GenerateTreeFromRoot();
            }
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
                    _mcts.GenerateTreeFromRoot(_hasOpponent ? _mctsOpponent.Root : null);
                }
            }
            else if (_hasOpponent && tree == _mctsOpponent)
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
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Bentengan
{
    public class Arena : Node2D
    {
        [Export]
        private string _teamPositioning;
        [Export]
        private int _length = 10;
        [Export]
        private int _height = 20;
        [Export]
        private float _distance = 42f;

        [Export(PropertyHint.Range)]
        private List<Team> _teams = new List<Team>(2);
        [Export(PropertyHint.Range)]
        private List<PersonPiece> _allPersons = new List<PersonPiece>();

        private BattleFlowManager _battleFlowManager;

        [Export]
        private Color _firstCellColor;
        [Export]
        private Color _secondCellColor;

        public List<Cell> Cells { get; } = new List<Cell>();
        public int Length => _length;
        public int Height => _height;
        public List<Team> Teams => _teams;
        public string TeamPositionings => _teamPositioning;
        
        public override void _Ready()
        {
            GD.Print("Ready: Arena");

            _battleFlowManager = GetNode<BattleFlowManager>("../BattleFlowManagers/Arena");
            GenerateCell();

            var playerTeam = GetNode<Team>($"../TeamPositionings/{_teamPositioning}/PlayerTeam");
            playerTeam.Init(this, true);
            _teams.Add(playerTeam);

            var aiTeam = GetNode<Team>($"../TeamPositionings/{_teamPositioning}/AITeam");
            aiTeam.Init(this);
            _teams.Add(aiTeam);

            _allPersons.AddRange(_teams[0].PersonPieces);
            _allPersons.AddRange(_teams[1].PersonPieces);

            SetTeamCellColor(_teams[0]);
            SetTeamCellColor(_teams[1]);

            UpdateAllPersonPieceInvalidMovement();
            
            GD.Print($"L:{_length}-H:{_height}");
        }

        #region BATTLE_SYSTEM
        public void UpdateAllPersonPieceLiveTime()
        {
            _teams.ForEach(t =>
                t.PersonPieces.ForEach(p => 
                {
                    if (t.CastleAreaCellIndex.Contains(p.CellPosition))
                        p.ResetLivetime();
                    else
                        p.IncreaseLivetime(1);
                }));
        }

        public void UpdateAllPersonPieceInvalidMovement()
        {
            foreach (var person in _allPersons)
            {
                _battleFlowManager.UpdateAllPersonPieceInvalidMovement(person.CellPosition, _length, _height, 
                    (int[] arr) => 
                    {
                        person.InvalidMovementArea = arr;
                        
                    });
            }
        }

        public void RegisterMove(int from, int to)
        {
            _battleFlowManager.RegisterMove(from, to);
        }

        public void UnregisterMove(int from)
        {
            if (_battleFlowManager.IsMoveRegistered(from))
                _battleFlowManager.UnregisterMove(from);
        }

        public void ExecuteAllPersonMoves()
        {
            // Teams.ForEach(t => 
            //     t.PersonPieces.ForEach(p => 
            //     {
            //         if (p.NextMove >= 0)
            //             p.ExecuteMove(Cells[p.NextMove]);
            //     }));

            _battleFlowManager.ExecuteRegisteredMove(ExecutePersonMove);
        }

        public void ExecutePersonMove(int from, int to)
        {
            PersonPiece person = _allPersons.Find(i => i.CellPosition == from);
            if (person.TrySetNextMove(to))
            {
                person.ExecuteMove(Cells[to]);
            }
            //person.IncreaseLivetime(1);
        }

        public void ExecuteSystematicPersonMove(int from, int to)
        {
            PersonPiece person = _allPersons.Find(i => i.CellPosition == from);
            person.SetArbitraryMove(to);
            person.ExecuteMove(Cells[to]);

            int team = person.TeamName.Equals(Teams[0].TeamName) ? 0 : 1;
            int opponent = team == 0 ? 1 : 0;
            if (_battleFlowManager.Evaluator.CheckIsInOpponentJail(to, Teams[opponent].JailAreaCellIndex.ToArray()))
            {
                person.IsCaptured = true;
            }
            else if (_battleFlowManager.Evaluator.CheckIsInOwnCastle(to, Teams[team].CastleAreaCellIndex.ToArray()))
            {
                person.IsCaptured = false;
                person.ResetLivetime();
            }
        }

        public void CheckWinningCondition()
        {
            bool firstTeamWin = BattleEvaluator.Instance.CheckCastleCapture(
                Teams[1].JailAreaCellIndex.ToArray(),
                Teams[0].PersonPieces.Select(i => i.CellPosition).ToArray());

            bool secondTeamWin = BattleEvaluator.Instance.CheckCastleCapture(
                Teams[0].JailAreaCellIndex.ToArray(),
                Teams[1].PersonPieces.Select(i => i.CellPosition).ToArray());

            if (!firstTeamWin && !secondTeamWin) return;
        }

        public void SendAllCapturedToJail()
        {
            // Teams[0].PersonPieces.ForEach(p1 =>
            // {
            //     Teams[1].PersonPieces.ForEach(p2 =>
            //         BattleEvaluator.Instance.CheckPersonPieceCapture(
            //             p1.ToData(), p2.ToData(),
            //             (int i) => 
            //             {
            //                 GD.Print($"Player capture one of MCTS {i}");
            //                 SendToJail(i);
            //             }));
            // });

            _battleFlowManager.SendAllCapturedToJail(
                Teams[0].PersonPieces.Select(p => p.ToData()).ToArray(),
                Teams[1].PersonPieces.Select(p => p.ToData()).ToArray(),
                ToData());

            _battleFlowManager.ExecuteSystematicMove(ExecuteSystematicPersonMove);
        }

        public void SendAllRescueeToCastle()
        {
            _battleFlowManager.SendAllRescueeToCastle(
                Teams[0].PersonPieces.Select(p => p.ToData()).ToArray(),
                Teams[1].PersonPieces.Select(p => p.ToData()).ToArray(),
                ToData());

            _battleFlowManager.ExecuteSystematicMove(ExecuteSystematicPersonMove);
        }

        public void SendToJail(int cell)
        {
            var person = Cells[cell].GetChild<PersonPiece>(2);
            List<int> jailArea = person.TeamName == Teams[0].TeamName ?
                Teams[1].JailAreaCellIndex : Teams[0].JailAreaCellIndex;

            int[] allPersonPosition = _allPersons.Select(s => s.CellPosition).ToArray();
            int emptyJail = BattleEvaluator.Instance.GetEmptyJail(jailArea.ToArray(), allPersonPosition);
            person.SetArbitraryMove(emptyJail);
            person.ExecuteMove(Cells[emptyJail]);

            GD.Print($"Jail Pos: {emptyJail}");
        }
        #endregion

        public void GenerateCell()
        {
            var cellSc = GD.Load<PackedScene>("res://Scenes/Cell.tscn");

            int i = 0;
            for (int row = 0; row < _height; row++)
            {
                for (int col = 0; col < _length; col++)
                {
                    Cell cell = cellSc.Instance() as Cell;
                    AddChild(cell);
                    cell.Index = i;
                    cell.Name = $"{Cell.CELL_PREFIX}_{i}";
                    Cells.Add(cell);

                    cell.Translate(new Vector2(col * _distance, row * _distance));

                    if (i % 2 == 0)
                    {
                        cell.Modulate = _secondCellColor;
                    }
                    else
                    {
                        cell.Modulate = _firstCellColor;
                    }
                    i++;
                }
            }
        }

        public void SetTeamCellColor(Team team)
        {
            Color castleColor = team.CastleAreaColor;
            foreach (int i in team.CastleAreaCellIndex)
            {
                Cells[i].Modulate = castleColor;
            };

            Color jailColor = team.JailAreaColor;
            foreach (int i in team.JailAreaCellIndex)
            {
                Cells[i].Modulate = jailColor;
            }
        }

        public void InsertChild(string cellName, Node node)
        {
            GetNode(cellName).AddChild(node);
        }

        public void InstantiatePersonPiece()
        {
            var sc = GD.Load<PackedScene>("res://Scenes/PersonPiece.tscn");
                    
            AddChild(InstantiateNode(sc));
        }

        public void ResetCellColor(int cellIndex)
        {
            Cells[cellIndex].Modulate = cellIndex % 2 == 0 ? _secondCellColor : _firstCellColor;

            SetTeamCellColor(_teams[0]);
            SetTeamCellColor(_teams[1]);
        }

        private Node InstantiateNode(PackedScene scene)
        {
            var node = scene.Instance();
            node.Name = "PersonPiece";

            return node;
        }

        public ArenaData ToData()
        {
            var data = new ArenaData();
            data.height = _height;
            data.length = _length;
            data.teamDatas = Teams.Select(i => i.ToData()).ToArray();
            data.personPieceDatas = _allPersons.Select(i => i.ToData()).ToArray();

            return data;
        }
    }

    public struct ArenaData
    {
        public int height;
        public int length;
        public TeamData[] teamDatas;
        public PersonPieceData[] personPieceDatas;
    }
}

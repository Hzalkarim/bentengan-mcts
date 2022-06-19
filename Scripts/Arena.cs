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
        public BattleFlowManager BattleFlowManager => _battleFlowManager;
        
        public override void _Ready()
        {
            GD.Print("Ready: Arena");

            _battleFlowManager = GetNode<BattleFlowManager>("../BattleFlowManager");
            GenerateCell();

            var playerTeam = GetNode<Team>($"../TeamPositionings/{_teamPositioning}/TopTeam");
            playerTeam.Init(this, true);
            _teams.Add(playerTeam);

            var aiTeam = GetNode<Team>($"../TeamPositionings/{_teamPositioning}/BottomTeam");
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

        public void RegisterMove(string teamName, int from, int to)
        {
            _battleFlowManager.RegisterMove(teamName, from, to);
        }

        public bool TryRegisterMove(string teamName, int from, int to)
        {
            var teamMove = _battleFlowManager.RegisteredMove.Where(m => m.teamName.Equals(teamName));
            var teamPrevPos = _teams.First(t => t.TeamName.Equals(teamName)).PersonPieces.Select(p => p.CellPosition);
            if (teamMove.Any(p => p.to == to))
            {
                return false;
            }

            if (teamPrevPos.Contains(to))
            {
                return false;
            }

            RegisterMove(teamName, from, to);
            return true;
        }

        public void UnregisterMove(int from)
        {
            if (_battleFlowManager.IsMoveRegistered(from))
                _battleFlowManager.UnregisterMove(from);
        }

        public void ExecuteAllPersonMoves()
        {
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
            PersonPiece person = null;
            PersonPiece[] duo = _allPersons.Where(p => p.CellPosition == from).ToArray();
            if (duo.Count() == 0)
            {
                GD.Print("Cell zero occupied");
                return;
            }
            else if (duo.Count() == 1)
            {
                GD.Print("Cell single occupied");
                person = duo[0];
            }
            else
            {
                GD.Print("Cell double occupied");
                IncreaseRed(from);
                if (duo[0].LiveTime == duo[1].LiveTime)
                {
                    SendToJail(duo[0]);
                    SendToJail(duo[1]);
                    GD.Print("Double send jail");

                    return;
                }
                else if (duo[0].LiveTime > duo[1].LiveTime)
                {
                    person = duo[0];
                    GD.Print("send jail 0");

                }
                else
                {
                    person = duo[1];
                    GD.Print("send jail 1");
                }
                //return;
            }

            if (person != null)
            {
                person.SetArbitraryMove(to);
                person.ExecuteMove(Cells[to]);
            }

            int team = person.TeamName.Equals(Teams[0].TeamName) ? 0 : 1;
            int opponent = team == 0 ? 1 : 0;
            if (_battleFlowManager.Evaluator.CheckIsInOpponentJail(to, Teams[opponent].JailAreaCellIndex.ToArray()))
            {
                IncreaseRed(from);
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
            SendToJail(person);
        }

        public void SendToJail(PersonPiece person)
        {
            List<int> jailArea = person.TeamName.Equals(Teams[0].TeamName) ?
                Teams[1].JailAreaCellIndex : Teams[0].JailAreaCellIndex;

            int[] allPersonPosition = _allPersons.Select(s => s.CellPosition).ToArray();
            int emptyJail = BattleEvaluator.Instance.GetEmptyJail(jailArea.ToArray(), allPersonPosition);
            if (emptyJail == -1)
                return;
            person.SetArbitraryMove(emptyJail);
            person.ExecuteMove(Cells[emptyJail]);

            person.IsCaptured = true;

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

        public void IncreaseRed(int idx)
        {
            Color c = Cells[idx].Modulate;
            int r = Mathf.Clamp(c.r8 + 100, 0, 255);
            GD.Print($"Increase Red: {r}");
            // Cells[idx].Modulate = Color.Color8((byte)r, (byte)c.g8, (byte)c.b8, (byte)c.a8);
            Cells[idx].Modulate = Color.Color8(255, 0, 0, 255);
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

        public void FillColorPrevCell()
        {
            var prev = _allPersons.Select(p => p.CellPosition);

            foreach (int i in prev)
            {
                Cells[i].Modulate = Color.Color8(200, 200, 255, 255);
            }
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

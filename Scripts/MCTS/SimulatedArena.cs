using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Bentengan;

namespace Bentengan.Mcts
{
    public class SimulatedArena : Node
    {
        private BattleFlowManager _battleFlowManager;
        private Arena _arena;

        private ArenaData _arenaData;
        private RandomNumberGenerator _rnd = new RandomNumberGenerator();
        private List<int> _tos;

        private uint _moveCount = 0;
        private uint _simulationCount = 0;
        private bool _isShowLog = false;
        private bool _isSimulating = false;
        private bool _isHighlightOccur = true;

        [Export]
        private bool _isActive = false;

        [Export]
        private int _teamMemberCount = 3;


        public override void _Ready()
        {
            GD.Print("Ready Simulated Arena");

            _battleFlowManager = GetNode<BattleFlowManager>("/root/Main/BattleFlowManagers/Simulated");
            _arena = GetNode<Arena>("/root/Main/Arena");
            _tos = new List<int>(_teamMemberCount);

            _battleFlowManager.gameplayHighlightEvent += OnGameplayHighlight;

            _arenaData = _arena.ToData();
        }

        public override void _Process(float delta)
        {
            if (!_isActive) return;

            StartSimulation((int)(5 / delta));
        }

        public void SetArenaData(ArenaData data)
        {
            _arenaData = data;
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

        public void StartSimulation(int times)
        {
            //GD.Print("Similation started");
            _arenaData = _arena.ToData();

            int i = 0;
            _rnd.Randomize();
            _isSimulating = true;
            while (_isSimulating && i < times)
            {
                i++;
                _moveCount++;
                if (_moveCount % 1000 == 0)
                {
                    GD.Print($"Total simulation {_simulationCount} with round runs: {_moveCount}");
                    _isShowLog = true;
                }

                RandomTeamMove(_arenaData.teamDatas[0].teamName);
                RandomTeamMove(_arenaData.teamDatas[1].teamName);
                
                ExecuteAllPersonMoves();
                UpdatePersonInvalidMovement();

                CastleCaptured();

                UpdatePersonLiveTime();

                SendRescueeToCastle();
                UpdatePersonInvalidMovement();

                SendCapturedToJail();
                UpdatePersonInvalidMovement();

            }

            if (i < times && !_isSimulating)
                StartSimulation(times - i);
            //GD.Print($"Simulation End. Round count: {i}");
        }

        public void RandomTeamMove(string teamName)
        {
            _tos.Clear();
            _rnd.Randomize();
            foreach (PersonPieceData person in _arenaData.personPieceDatas)
            {
                if (!person.teamName.Equals(teamName)) continue;

                int l = person.MovementArea.Length;
                if (l == 0) continue;

                int to = person.MovementArea[_rnd.RandiRange(0, l - 1)] + person.cellPosition;
                if (_tos.Contains(to)) 
                {
                    //GD.Print("Failed to move");
                    continue;
                }

                _tos.Add(to);
                RegisterMove(person.cellPosition, to);
                //GD.Print($"Register move {person.cellPosition}->{to}");
            }
        }

        private void CastleCaptured()
        {
            _battleFlowManager.CastleCaptured(_arenaData);
        }

        private void OnGameplayHighlight(string teamName, GameplayHighlight highlight)
        {
            _simulationCount++;
            _isSimulating = false;
            if (!_isShowLog) return;
            switch (highlight)
            {
                case GameplayHighlight.GameWon:
                    GD.Print($"{_simulationCount}:{_moveCount}: {teamName} WON");
                    break;
                case GameplayHighlight.PersonCaptured:
                    GD.Print($"{_simulationCount}:{_moveCount}: {teamName} captured");
                    break;
                case GameplayHighlight.PersonRescued:
                    GD.Print($"{_simulationCount}:{_moveCount}: {teamName} rescued");
                    break;
            }
            _isShowLog = false;
        }

        private void UpdatePersonLiveTime()
        {
            for (int i = 0; i < _arenaData.personPieceDatas.Length; i++)
            {
                int teamIdx = GetTeamIndex(_arenaData.personPieceDatas[i].teamName);
                bool isInCastle = _arenaData.teamDatas[teamIdx].castleArea
                    .Contains(_arenaData.personPieceDatas[i].cellPosition);

                if (isInCastle)
                {
                    _arenaData.personPieceDatas[i].liveTime = 0;
                }
                else
                {
                    _arenaData.personPieceDatas[i].liveTime += 1;
                }

            }
        }

        private void UpdatePersonInvalidMovement()
        {
            for (int i = 0; i < _arenaData.personPieceDatas.Length; i++)
            {
                _battleFlowManager.UpdateAllPersonPieceInvalidMovement(
                    _arenaData.personPieceDatas[i].cellPosition,
                    _arenaData.length, _arenaData.height,
                    (int[] arr) => 
                    {
                        _arenaData.personPieceDatas[i].invalidMovementArea = arr;
                    }
                );
            }
        }

        private void UpdateInvalidMovement(int[] arr, int idx)
        {
            _arenaData.personPieceDatas[idx].invalidMovementArea = arr;
        }

        private void ExecuteAllPersonMoves()
        {
            _battleFlowManager.ExecuteRegisteredMove(ExecutePersonMove);
        }

        private void ExecutePersonMove(int from, int to)
        {
            for (int i = 0; i < _arenaData.personPieceDatas.Length; i++)
            {
                if (_arenaData.personPieceDatas[i].cellPosition == from && !_arenaData.personPieceDatas[i].isCaptured)
                {
                    //GD.Print($"Movable Person at {from}");
                    //GD.Print(string.Join(", ", _arenaData.personPieceDatas[i].MovementArea));
                    if (_arenaData.personPieceDatas[i].MovementArea.Contains(from - to))
                    {
                        //GD.Print($"Executing move {_arenaData.personPieceDatas[i].cellPosition}->{to}");
                        _arenaData.personPieceDatas[i].cellPosition = to;
                    }
                    break;
                }
            }
        }

        private void ExecuteSystematicPersonMove(int from, int to)
        {
            for (int i = 0; i < _arenaData.personPieceDatas.Length; i++)
            {
                if (_arenaData.personPieceDatas[i].cellPosition == from)
                {
                    _arenaData.personPieceDatas[i].cellPosition = to;

                    int teamIdx = _arenaData.personPieceDatas[i].teamName.Equals(_arenaData.teamDatas[0].teamName) ? 0 : 1;
                    int opponentIdx = teamIdx == 0 ? 1 : 0;
                    if (_battleFlowManager.Evaluator.CheckIsInOpponentJail(to, _arenaData.teamDatas[opponentIdx].jailArea))
                    {
                        _arenaData.personPieceDatas[i].isCaptured = true;
                    }
                    else if (_battleFlowManager.Evaluator.CheckIsInOwnCastle(to, _arenaData.teamDatas[teamIdx].castleArea))
                    {
                        _arenaData.personPieceDatas[i].isCaptured = false;
                        _arenaData.personPieceDatas[i].liveTime = 0;
                    }
                    break;
                }
            }
        }

        private void SendRescueeToCastle()
        {
            _battleFlowManager.SendAllRescueeToCastle(
                _arenaData.personPieceDatas.Where(p => p.teamName.Equals(_arenaData.teamDatas[0].teamName)).ToArray(),
                _arenaData.personPieceDatas.Where(p => p.teamName.Equals(_arenaData.teamDatas[1].teamName)).ToArray(),
                _arenaData);

            _battleFlowManager.ExecuteSystematicMove(ExecuteSystematicPersonMove);
        }

        private void SendCapturedToJail()
        {
            _battleFlowManager.SendAllCapturedToJail(
                _arenaData.personPieceDatas.Where(p => p.teamName.Equals(_arenaData.teamDatas[0].teamName)).ToArray(),
                _arenaData.personPieceDatas.Where(p => p.teamName.Equals(_arenaData.teamDatas[1].teamName)).ToArray(),
                _arenaData);

            _battleFlowManager.ExecuteSystematicMove(ExecuteSystematicPersonMove);
        }

        private int GetTeamIndex(string teamName)
        {
            int idx = -1;
            for (int i = 0; i < _arenaData.teamDatas.Length; i++)
            {
                if (teamName.Equals(_arenaData.teamDatas[i].teamName))
                {
                    idx = i;
                    break;
                }
            }

            return idx;
        }
    }

    

}
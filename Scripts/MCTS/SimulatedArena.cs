using Godot;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Bentengan.Utility;

namespace Bentengan.Mcts
{
    public class SimulatedArena : Node
    {
        private BattleFlowManager _battleFlowManager;
        private Arena _arena;

        private ArenaData _arenaData;
        private RandomNumberGenerator _rnd = new RandomNumberGenerator();
        private SimulationSummary _summ = new SimulationSummary();
        private List<int> _tos;

        private uint _moveCount = 0;
        private uint _simulationCount = 0;
        private bool _isShowLog = false;
        private bool _isSimulating = false;

        private string _lastTeamHighlight;
        private GameplayHighlight _lastHighLight;

        [Export]
        private string _arenaPathFromRoot;
        [Export]
        private bool _isActive = false;
        [Export]
        private float _simulationFactorConstant;
        [Export]
        private string _summTeamName;
        [Export]
        private int _teamMemberCount = 3;

        public event Action onRoundStartEvent;
        public event Action onRoundEndEvent;
        public event Action onSimulationStartEvent;
        public event Action<string, GameplayHighlight> onSimulationEndHighlightedEvent;
        public event Action onSimulationEndWithNoHighlightEvent;

        public ArenaData ArenaData => _arenaData;
        public SimulationSummary Summary => _summ;
        public bool IsActive => _isActive;

        public override void _Ready()
        {
            GD.Print("Ready Simulated Arena");

            _battleFlowManager = GetNode<BattleFlowManager>("../BattleFlowManager");
            _arena = GetNode<Arena>($"{_arenaPathFromRoot}/Arena");
            _tos = new List<int>(_teamMemberCount);
            _summ.teamName = _summTeamName;

            _battleFlowManager.gameplayHighlightEvent += OnGameplayHighlight;

            _arenaData = _arena.ToData();
        }

        public void SetActive(bool isActive)
        {
            _isActive = isActive;
        }

        public void SetArenaData(ArenaData data)
        {
            _arenaData = data;
        }

        public void ResetArenaData()
        {
            _arenaData = _arena.ToData();
        }

        public void StopSimulation()
        {
            _isSimulating = false;
            _isActive = false;
        }

        public void AddGameplayHighlightListener(Action<string, GameplayHighlight> action)
        {
            _battleFlowManager.gameplayHighlightEvent += action;
        }

        public void RemoveGameplayHighlightListener(Action<string, GameplayHighlight> action)
        {
            _battleFlowManager.gameplayHighlightEvent -= action;
        }

        public void SetPersonData(int currentPos, int nextPos, int liveTime)
        {
            for (int i = 0; i < _arenaData.personPieceDatas.Length; i++)
            {
                if (_arenaData.personPieceDatas[i].cellPosition == currentPos)
                {
                    _arenaData.personPieceDatas[i].cellPosition = nextPos;
                    _arenaData.personPieceDatas[i].liveTime = liveTime;
                    break;
                }
            }
        }

        public void RegisterMove(string teamName, int from, int to)
        {
            _battleFlowManager.RegisterMove(teamName, from, to);
        }

        public bool TryRegisterMove(string teamName, int from, int to)
        {
            var teamMove = _battleFlowManager.RegisteredMove.Where(m => m.teamName.Equals(teamName));
            var teamPrevPos = _arenaData.personPieceDatas.Where(t => t.teamName.Equals(teamName)).Select(p => p.cellPosition);
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

        public void RunSingleRound()
        {
            onRoundStartEvent?.Invoke();

            ExecuteAllPersonMoves();
            UpdatePersonInvalidMovement();

            if (_isSimulating)
            {
                CheckCastleCaptured();            
                UpdatePersonLiveTime();
            }

            if (_isSimulating)
            {
                SendRescueeToCastle();
                UpdatePersonInvalidMovement();
            }

            if (_isSimulating)
            {
                SendCapturedToJail();
                UpdatePersonInvalidMovement();
            }


            onRoundEndEvent?.Invoke();
        }

        public void RunSingleRound(bool firstTeamRandom, bool secondTeamRandom)
        {
            if (firstTeamRandom)
                RegisterRandomTeamMove(_arenaData.teamDatas[0].teamName);
            if (secondTeamRandom)
                RegisterRandomTeamMove(_arenaData.teamDatas[1].teamName);

            RunSingleRound();
        }

        public void RunSimulation(int times)
        {
            if (!IsActive) return;

            int i = 0;
            _rnd.Randomize();
            _isSimulating = true;
            onSimulationStartEvent?.Invoke();
            while (_isSimulating && i < times)
            {
                i++;
                _moveCount++;
                if (_moveCount % 10000 == 0)
                {
                    GD.Print($"Total move counts: {_moveCount} - FPS {Engine.GetFramesPerSecond()}");
                    _isShowLog = true;
                }

                RunSingleRound(firstTeamRandom: true, secondTeamRandom: true);

            }

            if (!_isSimulating)
            {
                onSimulationEndHighlightedEvent?.Invoke(_lastTeamHighlight, _lastHighLight);
            }
            else
            {
                onSimulationEndWithNoHighlightEvent?.Invoke();
                _isSimulating = false;
            }
        }

        public void RegisterRandomTeamMove(string teamName)
        {
            _tos.Clear();
            _rnd.Randomize();
            foreach (PersonPieceData person in _arenaData.personPieceDatas)
            {
                if (!person.teamName.Equals(teamName)) continue;

                int l = person.MovementArea.Length;
                if (l == 0) continue;

                int to = -1;
                bool success = false;
                int i = 0;
                do
                {
                    i++;
                    to = person.MovementArea[_rnd.RandiRange(0, l - 1)] + person.cellPosition;
                    //to = person.cellPosition.CalculateStrategy((MctsStrategy)_rnd.RandiRange(0, 4), teamName);
                    if (_tos.Contains(to)) 
                    {
                        //GD.Print("Failed to move");
                        continue;
                    }

                    success = TryRegisterMove(person.teamName, person.cellPosition, to);
                }
                while (!success && i < 3);
                _tos.Add(to);
                //GD.Print($"Register move {person.cellPosition}->{to}");
            }
        }

        public string GetSummary() => _summ.ToString();

        private void CheckCastleCaptured()
        {
            _battleFlowManager.CheckCastleCaptured(_arenaData);
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

        private void OnGameplayHighlight(string teamName, GameplayHighlight highlight)
        {
            _simulationCount++;
            _isSimulating = false;
            _lastTeamHighlight = teamName;
            _lastHighLight = highlight;
            _summ.AddCount(highlight, teamName);
            if (!_isShowLog) return;
            switch (highlight)
            {
                case GameplayHighlight.GameWon:
                    GD.Print($"{_simulationCount}:{_moveCount}: {teamName} WON");
                    break;
                case GameplayHighlight.GameDraw:
                    GD.Print($"{_simulationCount}:{_moveCount}: {teamName} Draw");
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
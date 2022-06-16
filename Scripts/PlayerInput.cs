using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Bentengan.Mcts;

namespace Bentengan
{
    public class PlayerInput : Node
    {

        [Export]
        private string _selectingTeamName;
        [Export]
        private bool _isRandom;

        [Export]
        private bool _firstTeamUseAgent;
        [Export]
        private string _firstTeamAgentNodeName;
        [Export]
        private bool _secondTeamUseAgent;
        [Export]
        private string _secondTeamAgentNodeName;

        private Arena _arena;
        private AdversaryAgent _firstTeamAgent;
        private AdversaryAgent _secondTeamAgent;
        private SimulatedArena _simulatedArena;

        private Timer _timer;
        private Timer _roundTimer;
        private BaseButton _executeButton;

        private int _roundCount;
        private ProgressBar _timerProgressBar;

        private List<int> _validMoveArea = new List<int>();
        private PersonPiece _personPieceSelected;

        public PlayerInputPhase Phase { get; private set; } = PlayerInputPhase.Selecting;
        public string SelectingTeamName => _selectingTeamName;

        public override void _Ready()
        {
            //GD.Print("Ready: Player Input");
            _arena = GetNode<Arena>("../Arena");
            _arena.BattleFlowManager.gameplayHighlightEvent += OnGameplayHighlight;
            var cells = _arena.Cells;

            if (_firstTeamUseAgent)
                _firstTeamAgent = GetNode<AdversaryAgent>($"../Agents/{_firstTeamAgentNodeName}");
            if (_secondTeamUseAgent)
                _secondTeamAgent = GetNode<AdversaryAgent>($"../Agents/{_secondTeamAgentNodeName}");
            //_simulatedArena = GetNode<SimulatedArena>("../Agents/SimulatedArena");

            // foreach (Cell cell in cells)
            // {
            //     cell.OnClickedEvent += OnCellClicked;
            // }

            var buttons = GetNode("../Control/Buttons");
            buttons.GetNode<BaseButton>("SimulationTrigger").Connect("button_up", this, "OnSimulationTriggerButtonClicked");
            buttons.GetNode<BaseButton>("LogSummary").Connect("button_up", this, "OnLogSummaryButtonClicked");
            buttons.GetNode<BaseButton>("ResetSummary").Connect("button_up", this, "OnResetSummaryButtonClicked");
            _executeButton = buttons.GetNode<BaseButton>("Execute");
            _executeButton.Connect("button_up", this, "OnExecuteButtonClicked");

            _timer = buttons.GetNode<Timer>("../Timer");
            _timer.Connect("timeout", this, "OnTimerTick");
            _roundTimer = buttons.GetNode<Timer>("../RoundTimer");


            _timerProgressBar = buttons.GetNode<ProgressBar>("../TimerProgress");
            _timerProgressBar.Value = 0;

            if (_firstTeamUseAgent)
                _firstTeamAgent.Mcts.onBackpropagationEndEvent += OnRoundEnd;
            if (_firstTeamUseAgent)
                _firstTeamAgent.GenerateTree();
            if (_secondTeamUseAgent)
                _secondTeamAgent.GenerateTree();

            _timer.Start();
        }

        // private void OnPlayerButtonClicked()
        // {
        //     _selectingTeamName = "Player";
        // }

        // private void OnAiButtonClicked()
        // {
        //     _selectingTeamName = "MCTS";
        // }

        private void OnSimulationTriggerButtonClicked()
        {
            _simulatedArena.SetActive(!_simulatedArena.IsActive);
        }

        private void OnLogSummaryButtonClicked()
        {
            string log = _simulatedArena.GetSummary();
            GD.Print(log);
        }

        private void OnResetSummaryButtonClicked()
        {
            var summ =  _simulatedArena.Summary;
            summ.ResetCount();
            GD.Print($"Summary for {summ.teamName} has been reset.");
        }

        private void OnExecuteButtonClicked()
        {
            RegisterAndUpdateMovement();
        }

        private void RegisterAndUpdateMovement()
        {
            _executeButton.Disabled = true;
            _timer.Start();
            //_opponentAgent.RegisterRandomMove();

            if (_firstTeamUseAgent)
                _firstTeamAgent.RegisterBestMove();
            if (_secondTeamUseAgent)
                _secondTeamAgent.RegisterBestMove();
            // if (_isRandom)
            //     _playerAgent.RegisterRandomMove();

            //_arena.FillColorPrevCell();

            _arena.ExecuteAllPersonMoves();
            _arena.UpdateAllPersonPieceInvalidMovement();
            
            _arena.UpdateAllPersonPieceLiveTime();

            _arena.BattleFlowManager.CheckCastleCaptured(_arena.ToData());

            _roundTimer.Connect("timeout", this, "UpdateRescuee", flags: 4);
            _roundTimer.Start();
        }

        private void UpdateRescuee()
        {
            _arena.SendAllRescueeToCastle();
            _arena.UpdateAllPersonPieceInvalidMovement();

            _roundTimer.Connect("timeout", this, "UpdateCaptured", flags: 4);
            _roundTimer.Start();
        }
        private void UpdateCaptured()
        {
            _arena.SendAllCapturedToJail();
            _arena.UpdateAllPersonPieceInvalidMovement();

            _arena.BattleFlowManager.CheckTeamEliminated(_arena.ToData());

            _roundTimer.Connect("timeout", this, "UpdateAiTree", flags: 4);
            _roundTimer.Start();
        }
        private void UpdateAiTree()
        {
            OnSimStart();
            if (_firstTeamUseAgent)
                _firstTeamAgent.GenerateTree();
            if (_secondTeamUseAgent)
                _secondTeamAgent.GenerateTree();
        }

        private void OnCellClicked(Cell cell)
        {

            if (Phase == PlayerInputPhase.PersonPieceSelected)
            {
                if (cell.GetNodeOrNull($"{SelectingTeamName}_{Team.TEAM_PERSON_SUFFIX}") != null)
                {
                    //GD.Print("Other PersonPiece of same team exist");
                    return;
                }

                //GD.Print(cell.Index - _personPieceSelected.CellPosition);
                if (_validMoveArea.Contains(cell.Index))
                {
                    //GD.Print($"Can Move from {_personPieceSelected.CellPosition} to {cell.Index}");
                    //_personPieceSelected.TrySetNextMove(cell.Index);
                    _arena.UnregisterMove(_personPieceSelected.CellPosition);
                    _arena.TryRegisterMove(_personPieceSelected.TeamName, _personPieceSelected.CellPosition, cell.Index);
                }

                Phase = PlayerInputPhase.Selecting;
                foreach (var moveArea in _validMoveArea)
                {
                    _arena.ResetCellColor(moveArea);
                }
                _validMoveArea.Clear();
            }
            else if (Phase == PlayerInputPhase.Selecting)
            {
                //GD.Print("HEHEH");
                PersonPiece person = 
                    cell.GetNodeOrNull<PersonPiece>($"{SelectingTeamName}_{Team.TEAM_PERSON_SUFFIX}");
                if (person == null) return;

                _personPieceSelected = person;
                //GD.Print($"{cell.Name} - {person.Name} - {person.CellPosition}");

                var move = person.MovementArea;
                
                foreach (int i in move)
                {
                    int moveAreaCellIndex = person.CellPosition + i;
                    _validMoveArea.Add(moveAreaCellIndex);
                    _arena.Cells[moveAreaCellIndex].Modulate = Color.Color8(100, 100, 100, 255);
                }

                Phase = PlayerInputPhase.PersonPieceSelected;
            }
        }

        private void OnTimerTick()
        {
            //_executeButton.Disabled = false;
            OnExecuteButtonClicked();
        }

        private void OnSimStart()
        {
            _timerProgressBar.Value = 0;
            _roundCount = 0;
        }

        private void OnRoundEnd(MonteCarloTreeSearch mcts)
        {
            _roundCount++;
            _timerProgressBar.Value = _roundCount * 100 / _firstTeamAgent.Mcts.LimitVisit;
        }

        private void OnGameplayHighlight(string team, GameplayHighlight highlight)
        {
            GD.Print($"{team} {highlight.ToString()}");
        }
    }

    public enum PlayerInputPhase
    {
        Selecting, PersonPieceSelected
    }

}
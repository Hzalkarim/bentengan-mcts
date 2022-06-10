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

        private Arena _arena;
        private AdversaryAgent _opponentAgent;
        private AdversaryAgent _playerAgent;
        private SimulatedArena _simulatedArena;

        private Timer _timer;
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
            var cells = _arena.Cells;

            _opponentAgent = GetNode<AdversaryAgent>("../Agents/OpponentAgent");
            //_playerAgent = GetNode<AdversaryAgent>("../Agents/PlayerAgent");
            _simulatedArena = GetNode<SimulatedArena>("../Agents/SimulatedArena");

            foreach (Cell cell in cells)
            {
                cell.OnClickedEvent += OnCellClicked;
            }

            var buttons = GetNode("../Control/Buttons");
            buttons.GetNode<BaseButton>("SimulationTrigger").Connect("button_up", this, "OnSimulationTriggerButtonClicked");
            buttons.GetNode<BaseButton>("LogSummary").Connect("button_up", this, "OnLogSummaryButtonClicked");
            buttons.GetNode<BaseButton>("ResetSummary").Connect("button_up", this, "OnResetSummaryButtonClicked");
            _executeButton = buttons.GetNode<BaseButton>("Execute");
            _executeButton.Connect("button_up", this, "OnExecuteButtonClicked");

            _timer = buttons.GetNode<Timer>("../Timer");
            _timer.Connect("timeout", this, "OnTimerTick");

            _timerProgressBar = buttons.GetNode<ProgressBar>("../TimerProgress");
            _timerProgressBar.Value = 0;

            _opponentAgent.Mcts.onBackpropagationEndEvent += OnRoundEnd;

            _opponentAgent.GenerateTree();
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
            _executeButton.Disabled = true;
            _timer.Start();
            //_opponentAgent.RegisterRandomMove();
            _opponentAgent.RegisterBestMove();
            // if (_isRandom)
            //     _playerAgent.RegisterRandomMove();

            //_arena.FillColorPrevCell();

            _arena.ExecuteAllPersonMoves();
            _arena.UpdateAllPersonPieceInvalidMovement();
            
            _arena.UpdateAllPersonPieceLiveTime();

            _arena.SendAllRescueeToCastle();
            _arena.UpdateAllPersonPieceInvalidMovement();

            _arena.SendAllCapturedToJail();
            _arena.UpdateAllPersonPieceInvalidMovement();

            OnSimStart();
            _opponentAgent.GenerateTree();
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
            _executeButton.Disabled = false;

        }

        private void OnSimStart()
        {
            _timerProgressBar.Value = 0;
            _roundCount = 0;
        }

        private void OnRoundEnd()
        {
            _roundCount++;
            _timerProgressBar.Value = _roundCount * 100 / _opponentAgent.Mcts.LimitVisit;
        }
    }

    public enum PlayerInputPhase
    {
        Selecting, PersonPieceSelected
    }

}
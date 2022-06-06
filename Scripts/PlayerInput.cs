using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

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

        private List<int> _validMoveArea = new List<int>();
        private PersonPiece _personPieceSelected;

        public PlayerInputPhase Phase { get; private set; } = PlayerInputPhase.Selecting;
        public string SelectingTeamName => _selectingTeamName;

        public override void _Ready()
        {
            GD.Print("Ready: Player Input");
            _arena = GetNode<Arena>("../Arena");
            var cells = _arena.Cells;

            _opponentAgent = GetNode<AdversaryAgent>("../Agents/OpponentAgent");
            _playerAgent = GetNode<AdversaryAgent>("../Agents/PlayerAgent");

            foreach (Cell cell in cells)
            {
                cell.OnClickedEvent += OnCellClicked;
            }

            var buttons = GetNode("../Control/Buttons");
            buttons.GetNode<BaseButton>("PlayerSelect").Connect("button_up", this, "OnPlayerButtonClicked");
            buttons.GetNode<BaseButton>("AISelect").Connect("button_up", this, "OnAiButtonClicked");
            buttons.GetNode<BaseButton>("Execute").Connect("button_up", this, "OnExecuteButtonClicked");
        }

        private void OnPlayerButtonClicked()
        {
            _selectingTeamName = "Player";
        }

        private void OnAiButtonClicked()
        {
            _selectingTeamName = "MCTS";
        }

        private void OnExecuteButtonClicked()
        {
            
            _opponentAgent.RegisterRandomMove();
            if (_isRandom)
                _playerAgent.RegisterRandomMove();

            _arena.ExecuteAllPersonMoves();
            _arena.UpdateAllPersonPieceInvalidMovement();
            
            _arena.UpdateAllPersonPieceLiveTime();

            _arena.SendAllRescueeToCastle();
            _arena.UpdateAllPersonPieceInvalidMovement();

            _arena.SendAllCapturedToJail();
            _arena.UpdateAllPersonPieceInvalidMovement();

        }

        private void OnCellClicked(Cell cell)
        {

            if (Phase == PlayerInputPhase.PersonPieceSelected)
            {
                if (cell.GetNodeOrNull($"{SelectingTeamName}_{Team.TEAM_PERSON_SUFFIX}") != null)
                {
                    GD.Print("Other PersonPiece of same team exist");
                    return;
                }

                GD.Print(cell.Index - _personPieceSelected.CellPosition);
                if (_validMoveArea.Contains(cell.Index))
                {
                    GD.Print($"Can Move from {_personPieceSelected.CellPosition} to {cell.Index}");
                    //_personPieceSelected.TrySetNextMove(cell.Index);
                    _arena.UnregisterMove(_personPieceSelected.CellPosition);
                    _arena.RegisterMove(_personPieceSelected.CellPosition, cell.Index);
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
                GD.Print("HEHEH");
                PersonPiece person = 
                    cell.GetNodeOrNull<PersonPiece>($"{SelectingTeamName}_{Team.TEAM_PERSON_SUFFIX}");
                if (person == null) return;

                _personPieceSelected = person;
                GD.Print($"{cell.Name} - {person.Name} - {person.CellPosition}");

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
    }

    public enum PlayerInputPhase
    {
        Selecting, PersonPieceSelected
    }

}
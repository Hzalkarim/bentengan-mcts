using Godot;
using System.Linq;

namespace Bentengan
{

    public class PersonPiece : Node2D
    {
        [Export]
        private int _liveTime = 0;
        [Export]
        private int _cellPosition = 0;
        private int[] _movementArea = {-1, 0, 1};
        private int[] _invalidMovementArea = {};
        private int[] _captureArea = {0};

        private bool _isCaptured;

        private int _nextMove = -1;

        private Sprite _isCapturedSprite;

        #region PROPERTY
        public string TeamName {get; set;}
        public bool IsCaptured 
        {
            get
            {
                return _isCaptured;
            }
            set
            {
                if (value)
                {
                    _isCapturedSprite.Show();
                }
                else
                {
                    _isCapturedSprite.Hide();
                }
                _isCaptured = value;
            }
        }
        public int NextMove => _nextMove;
        public int LiveTime
        {
            get
            {
                return _liveTime;
            }
        }
        public int CellPosition
        {
            get 
            {
                return _cellPosition;
            }
            set
            {
                _cellPosition = value;
            }
        }
        public int[] CaptureArea
        {
            get
            {
                return _captureArea.Except(_invalidMovementArea).ToArray();
            }
            set
            {
                _captureArea = value;
            }
        }
        public int[] MovementArea
        {
            get { return _movementArea.Except(_invalidMovementArea).ToArray(); }
            set { _movementArea = value; }
        }
        public int[] InvalidMovementArea
        {
            get
            {
                return _invalidMovementArea;
            }
            set 
            {
                _invalidMovementArea = value;
            }
        }
        #endregion

        public override void _Ready()
        {
            _isCapturedSprite = GetNode<Sprite>("IsCaptured");
            if (!_isCaptured)
                _isCapturedSprite.Hide();
        }

        public void ResetLivetime()
        {
            _liveTime = 0;
        }

        public void IncreaseLivetime(int livetime)
        {
            _liveTime += livetime;
        }

        public void SetIsCaptured(bool val)
        {
            _isCaptured = val;
        }

        public bool TrySetNextMove(int nextCell)
        {
            if (IsCaptured) return false;

            int relativeCell = nextCell - CellPosition;
            if (!_movementArea.Contains(relativeCell) || _invalidMovementArea.Contains(relativeCell)) return false;

            _nextMove = nextCell;
            //GD.Print($"PersonPiece Next move set to {_nextMove}");
            return true;
        }

        public void SetArbitraryMove(int cellIndex)
        {
            _nextMove = cellIndex;
        }

        public void ResetNextMove()
        {
            _nextMove = -1;
        }

        public void ExecuteMove(Cell cell = null)
        {
            if (_nextMove == -1) return;
            if (cell != null)
            {
                GetParent().RemoveChild(this);
                cell.AddChild(this);
            }
            CellPosition = _nextMove;
            ResetNextMove();
        }

        public PersonPieceData ToData()
        {
            var data = new PersonPieceData();
            data.teamName = TeamName;
            data.liveTime = _liveTime;
            data.isCaptured = IsCaptured;
            data.cellPosition = _cellPosition;
            data.movementArea = _movementArea;
            data.invalidMovementArea = _invalidMovementArea;
            data.captureArea = _captureArea;

            return data;
        }
    }

    public struct PersonPieceData
    {
        public string teamName;
        public int liveTime;
        public bool isCaptured;
        public int cellPosition;
        public int[] movementArea;
        public int[] invalidMovementArea;
        public int[] captureArea;

        public int[] MovementArea => movementArea.Except(invalidMovementArea).ToArray();
        public int[] CaptureArea => captureArea.Except(invalidMovementArea).ToArray();
    }
}

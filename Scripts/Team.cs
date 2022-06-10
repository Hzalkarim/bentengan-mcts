using Godot;
using System.Collections.Generic;

namespace Bentengan
{
    public class Team : Node2D
    {
        public const string TEAM_PERSON_SUFFIX = "PersonPiece";

        private List<PersonPiece> _personPieces = new List<PersonPiece>();

        [Export]
        private string _teamName;

        [Export]
        private Color _castleAreaColor;
        [Export(PropertyHint.Range)]
        private List<int> _castleAreaCellIndex;

        [Export]
        private Color _jailAreaColor;
        [Export(PropertyHint.Range)]
        private List<int> _jailAreaCellIndex;
        
        [Export(PropertyHint.Range)]
        private List<int> _initPersonPiecePos;

        public List<PersonPiece> PersonPieces => _personPieces;
        public string TeamName => _teamName;
        public Color CastleAreaColor => _castleAreaColor;
        public List<int> CastleAreaCellIndex => _castleAreaCellIndex;
        public Color JailAreaColor => _jailAreaColor;
        public List<int> JailAreaCellIndex => _jailAreaCellIndex;

        public void Init(Arena arena, bool invert = false)
        {
            var persons = new List<PersonPiece>(3);
            int l = arena.Length;

            var personPiecePackedScene = GD.Load<PackedScene>("res://Scenes/PersonPiece.tscn");
            for (int i = 0; i < _initPersonPiecePos.Count; i++)
            {
                var person = personPiecePackedScene.Instance<PersonPiece>();
                person.Name = $"{TeamName}_{TEAM_PERSON_SUFFIX}";
                person.TeamName = TeamName;
                person.CellPosition = _initPersonPiecePos[i];
                person.MovementArea = new int[]
                {
                    l - 1,  l, l + 1,
                    -1, 0, 1,
                    -l - 1, -l, -l + 1
                };
                person.InvalidMovementArea = new int[12];
                person.CaptureArea = new int[]
                {
                    l,
                    -1, 0, 1,
                    -l
                };
                persons.Add(person);

                person.Scale = new Vector2(.4f, .4f);
                if (invert)
                    person.RotationDegrees = 180f;

                string nextPos = $"{Cell.CELL_PREFIX}_{_initPersonPiecePos[i]}";
                arena.InsertChild(nextPos, person);
                
            }

            GD.Print($"Generate Team, l:{l}");
            AddPersonPiece(persons);
        }

        public void AddPersonPiece(List<PersonPiece> persons)
        {
            _personPieces.AddRange(persons);
        }

        public PersonPiece GetPersonPiece(int index)
        {
            return _personPieces[index];
        }

        public TeamData ToData()
        {
            TeamData data = new TeamData();
            data.teamName = _teamName;
            data.castleArea = _castleAreaCellIndex.ToArray();
            data.jailArea = _jailAreaCellIndex.ToArray();

            return data;
        }
    }

    public struct TeamData
    {
        public string teamName;
        public int[] castleArea;
        public int[] jailArea;
    }

}
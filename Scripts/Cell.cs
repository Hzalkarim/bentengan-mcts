using Godot;
using System;

namespace Bentengan
{
    public class Cell : StaticBody2D
    {
        public const string CELL_PREFIX = "Cell";
        public event Action<Cell> OnClickedEvent;

        public int Index { get; set; }
        public AreaType AreaType { get; set; }

        public override void _Ready()
        {
            InputPickable = true;
            Connect("input_event", this, "OnClicked");
        }

        public void ConnectInputEvent(Godot.Object target, string method)
        {
            Connect("input_event", target, method);
        }

        public void DisconnectInputEvent(Godot.Object target, string method)
        {
            Disconnect("input_event", target, method);
        }

        private void OnClicked(Node viewport, InputEvent ev, int shapeIdx)
        {

            if (ev is InputEventMouseButton evBtn && evBtn.Pressed)
            {
                OnClickedEvent?.Invoke(this);

                //Disconnect("input_event", this, "OnClicked");
            }
        }
    }

    public enum AreaType
    {
        Normal, Castle, Jail
    }
}
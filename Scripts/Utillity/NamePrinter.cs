using Godot;
using System;

namespace Bentengan.Utility
{
    public class NamePrinter : Node2D
    {
        public override void _Ready()
        {
            GD.Print($"Root: {GetPath()} - Print: {Name}");
        }
    }

}
using Godot;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Bentengan.Utility;

namespace Bentengan.Mcts
{
    public class SimulatedArenaManager : Node2D
    {
        private Queue<SimulatedArena> _sim = new Queue<SimulatedArena>();

        [Export]
        private int _processFactor;
        [Export]
        private int _simPerProcessLimit;

        public override void _Process(float delta)
        {
            if (_sim.Count == 0) return;

            int times = (int)Mathf.Clamp( _processFactor / delta, 0, 200);
            int i = 0;
            while (i < _simPerProcessLimit && _sim.Count > 0)
            {
                i++;
                _sim.Dequeue().RunSimulation(times);
            }
        }

        public void QueueSimulation(SimulatedArena arena)
        {
            _sim.Enqueue(arena);
        }
    }

}
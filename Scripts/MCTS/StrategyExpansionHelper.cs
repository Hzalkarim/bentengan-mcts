using System;
using System.Linq;
using System.Collections.Generic;

namespace Bentengan.Utility
{
    public class StrategyExpansionHelper
    {
        private static StrategyExpansionHelper _instance;
        public static StrategyExpansionHelper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new StrategyExpansionHelper();
                return _instance;
            }
        }

        private IEnumerable<MctsStrategy[]> _roleBasedStrategy;
        private IEnumerable<MctsStrategy[]> _roleBasedOnePersonStrategy;
        private IEnumerable<MctsStrategy[]> _completeStrategy;
        private IEnumerable<MctsStrategy[]> _completeOnePersonStrategy;

        public IEnumerable<MctsStrategy[]> RoleBased => _roleBasedStrategy;
        public IEnumerable<MctsStrategy[]> RoleBasedOnePerson => _roleBasedOnePersonStrategy;
        public IEnumerable<MctsStrategy[]> Complete => _completeStrategy;
        public IEnumerable<MctsStrategy[]> CompleteOnePerson => _completeOnePersonStrategy;

        private StrategyExpansionHelper()
        {
            MctsStrategy[] all = new MctsStrategy[5]
            {
                MctsStrategy.CaptureOpponent,
                MctsStrategy.CaptureCastle,
                MctsStrategy.BackToCastle,
                MctsStrategy.AvoidOpponent,
                MctsStrategy.RescueTeam,
            };

            MctsStrategy[] atk = new MctsStrategy[3]
            {
                MctsStrategy.CaptureOpponent,
                MctsStrategy.CaptureCastle,
                MctsStrategy.BackToCastle,
                //MctsStrategy.AvoidOpponent,
                //MctsStrategy.RescueTeam,
            };

            MctsStrategy[] mid = new MctsStrategy[3]
            {
                //MctsStrategy.CaptureOpponent,
                //MctsStrategy.CaptureCastle,
                MctsStrategy.AvoidOpponent,
                MctsStrategy.BackToCastle,
                MctsStrategy.RescueTeam,
            };

            MctsStrategy[] def = new MctsStrategy[2]
            {
                MctsStrategy.CaptureOpponent,
                //MctsStrategy.CaptureCastle,
                //MctsStrategy.AvoidOpponent,
                MctsStrategy.BackToCastle,
                //MctsStrategy.RescueTeam,
            };

            _roleBasedStrategy = from x in def from y in mid from z in atk select new MctsStrategy[3] {x, y, z};
            _roleBasedOnePersonStrategy = from x in def select new MctsStrategy[1] {x};
            _completeStrategy = from x in all from y in all from z in all select new MctsStrategy[3] {x, y, z};
            _completeOnePersonStrategy = from x in all select new MctsStrategy[1] {x};
        }
    }
}
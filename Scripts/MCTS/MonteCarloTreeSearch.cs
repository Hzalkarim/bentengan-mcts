using Godot;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Bentengan.Utility;

namespace Bentengan.Mcts
{
    public class MonteCarloTreeSearch : Node
    {
        private SimulatedArena _simulatedArena;
        private IEnumerable<MctsStrategy[]> _strategyCross;

        private int _frameQuota;
        private MctsNode _nodeToSimulate;

        [Export]
        private string _teamName;
        [Export]
        private int _limitVisit;
        [Export]
        private bool _initOnReady;
        [Export]
        private float _processQuotaFactor;

        public MctsNode Root { get; set; }
        public bool IsActive { get; private set; }

        public int LimitVisit 
        {
            get { return _limitVisit; }
            set { _limitVisit = value; }
        }

        public event Action onFinishGenerateTreeEvent;
        public event Action onBackpropagationEndEvent;

        public override void _Ready()
        {
            if (_initOnReady)
                Init();
            //IsActive = true;
            // _simulatedArena.ResetArenaData();
            // Selection(Root);
            //Selection(RootAdv);

        }

        public void Init()
        {
            Root = new MctsNode();
            MctsStrategy[] hehe = new MctsStrategy[4]
            {
                MctsStrategy.CaptureCastle,
                MctsStrategy.CaptureOpponent,
                MctsStrategy.BackToCastle,
                MctsStrategy.RescueTeam
            };

            _strategyCross = from x in hehe from y in hehe from z in hehe select new MctsStrategy[3] {x, y, z};

            _simulatedArena = GetNode<SimulatedArena>("../../SimulatedArena");
            
        }

        public void RegisterToSimulatedArenaEvents()
        {
            _simulatedArena.onRoundStartEvent += OnRoundStart;
            _simulatedArena.onSimulationEndWithNoHighlightEvent += onSimulationEndWithNoHighlight;
            _simulatedArena.AddGameplayHighlightListener(OnGameplayHighlight);
        }

        public void UnregisterToSimulatedArenaEvents()
        {
            _simulatedArena.onRoundStartEvent -= OnRoundStart;
            _simulatedArena.onSimulationEndWithNoHighlightEvent -= onSimulationEndWithNoHighlight;
            _simulatedArena.RemoveGameplayHighlightListener(OnGameplayHighlight);
        }

        public override void _Process(float delta)
        {
            if (!IsActive) return;

            _frameQuota = (int) (_processQuotaFactor / delta);
            //GD.Print($"Process-Frame quota {_frameQuota}");
            _simulatedArena.RunSimulation(_frameQuota);
        }

        public void StopSimulation()
        {
            _simulatedArena.StopSimulation();
        }

        public string GetMctsPath(MctsNode node)
        {
            var strBuilder = new StringBuilder();
            AppendMctsName(node, strBuilder);

            return strBuilder.ToString();
        }

        public MctsNode GetMaxUctChildNode(MctsNode parentNode)
        {
            int maxIdx = -1;
            double max = double.MinValue;
            for (int i = 0; i < parentNode.childs.Count; i++)
            {
                if (parentNode.childs[i].timesVisit == 0)
                    continue;
                double uct = UpperConfidenceBoundForTree(parentNode.childs[i].AverageScore,
                    parentNode.timesVisit, parentNode.childs[i].timesVisit);
                if (uct > max)
                {
                    max = uct;
                    maxIdx = i;
                }
            }
            return parentNode.childs[maxIdx];
        }

        public MctsNode GetMaxAvgChildNode(MctsNode parentNode)
        {
            float maxAvg = parentNode.childs.Max(n => n.AverageScore);
            return parentNode.childs.First(n => n.AverageScore == maxAvg);
        }

        public MctsNode GetUnvisitedChildNode(MctsNode parentNode)
        {
            return parentNode.childs.First(c => c.timesVisit == 0);
        }

        public void GenerateTreeFromRoot()
        {
            _simulatedArena.ResetArenaData();
            Selection(Root);
        }

        private void Selection(MctsNode node, Action opponentMove = null)
        {
            if (node.parent != null)
            {
                int[] pos = _simulatedArena.ArenaData.personPieceDatas
                    .Where(p => p.teamName.Equals(_teamName))
                    .Select(i => i.cellPosition).ToArray();
                for (int i = 0; i < pos.Length; i++)
                {
                    _simulatedArena.TryRegisterMove(_teamName, pos[i], node.registeredMove[i]);
                }

                if (opponentMove == null)
                {
                    string oppTeam = _simulatedArena.ArenaData.teamDatas[0].teamName.Equals(_teamName) ? 
                        _simulatedArena.ArenaData.teamDatas[1].teamName :
                        _simulatedArena.ArenaData.teamDatas[0].teamName;

                    StrategyAlgorithmCalculator.SetArenaData(_simulatedArena.ArenaData);
                    _simulatedArena.RegisterRandomTeamMove(oppTeam);
                }
                else
                {
                    opponentMove.Invoke();
                }
                _simulatedArena.RunSingleRound();
            }

            if (node.childs == null || node.childs.Count == 0)
            {
                Expansion(node);
                return;
            }


            MctsNode nextNode = node.timesVisit < node.childs.Count ?
                GetUnvisitedChildNode(node) : GetMaxUctChildNode(node);
            //GD.Print($"Selecting: {nextNode.ToString()}");
            Selection(nextNode, opponentMove);
        }

        private void Expansion(MctsNode node)
        {
            var persons = _simulatedArena.ArenaData.personPieceDatas.Where(p => p.teamName.Equals(_teamName)).ToArray();
            StrategyAlgorithmCalculator.SetArenaData(_simulatedArena.ArenaData);

            foreach (MctsStrategy[] strat in _strategyCross)
            {
                if (persons.All(p => !p.isCaptured) && strat.Contains(MctsStrategy.RescueTeam))
                    continue;

                var moveReg = new int[strat.Length];
                for (int i = 0; i < strat.Length; i++)
                {

                    moveReg[i] = persons[i].cellPosition.CalculateStrategy(strat[i], _teamName);
                }
                node.AddChild(moveReg, 0f, 0);
            }

            Simulation(node.childs[0]);
        }

        private void Simulation(MctsNode node)
        {
            var teamPersons = _simulatedArena.ArenaData.personPieceDatas
                .Where(p => p.teamName.Equals(_teamName))
                .Select(p => p.cellPosition).ToArray();

            for (int i = 0; i < teamPersons.Length; i++)
            {
                _simulatedArena.TryRegisterMove(_teamName, teamPersons[i], node.registeredMove[i]);
            }
            
            _nodeToSimulate = node;
            IsActive = true;
        }

        private void Backpropagation(MctsNode node, float scoreUpdate)
        {
            node.score += scoreUpdate;
            node.timesVisit++;

            if (node.parent != null)
            {
                Backpropagation(node.parent, scoreUpdate);
            }
            else
            {
                onBackpropagationEndEvent?.Invoke();
                if (_frameQuota <= 0)
                {
                    if (Root.timesVisit < _limitVisit)
                    {
                        IsActive = true;
                    }
                    return;
                }

                if (node.timesVisit < _limitVisit)
                {
                    _simulatedArena.ResetArenaData();
                    Selection(Root);
                }
                else if (node.timesVisit == _limitVisit)
                {
                    onFinishGenerateTreeEvent?.Invoke();
                    // GD.Print($"AvgScore: {node.AverageScore} - Times Visit: {node.timesVisit}");
                    // MctsNode max = GetMaxScoreChildNode(Root);
                    // GD.Print($"Best Move: {max.ToString()} with avgscore {max.AverageScore} visited {max.timesVisit}");
                }
            }
        }

        private double UpperConfidenceBoundForTree(float score, int visit, int toBeVisiting)
        {
            return (score / visit) + Math.Sqrt(2 * Math.Log(visit) / toBeVisiting);
        }

        private void AppendMctsName(MctsNode node, StringBuilder stringBuilder, string separator = "/")
        {
            GD.Print(node == null);
            if (node == null) return;
            if (node.parent != null)
                AppendMctsName(node.parent, stringBuilder, separator);

            stringBuilder.Append(node.registeredMove.ToString());
            stringBuilder.Append(separator);
        }

        private void OnGameplayHighlight(string teamName, GameplayHighlight highlight)
        {
            IsActive = false;
            switch (highlight)
            {
                case GameplayHighlight.GameWon:
                    Backpropagation(_nodeToSimulate, teamName.Equals(_teamName) ? 1 : -.5f);
                    break;
                case GameplayHighlight.GameDraw:
                    Backpropagation(_nodeToSimulate, 0);
                    break;
                case GameplayHighlight.PersonCaptured:
                    Backpropagation(_nodeToSimulate, teamName.Equals(_teamName) ? -.9f : .5f);
                    break;
                case GameplayHighlight.PersonRescued:
                    Backpropagation(_nodeToSimulate, teamName.Equals(_teamName) ? .9f : -.3f);
                    break;
            }
        }

        private void OnRoundStart()
        {
            _frameQuota--;
        }

        private void onSimulationEndWithNoHighlight()
        {
            //Selection(Root);
            IsActive = false;
            //GD.Print($"OnSimulationEnd-Frame quota {_frameQuota}");
            // if (_frameQuota <= 0)
            //     IsActive = true;

            if (_nodeToSimulate == null) return;
            Backpropagation(_nodeToSimulate, 0f);
        }
    }

    public class MctsNode
    {
        public int[] registeredMove;
        public int[] personsLiveTime;

        public float score;
        public int timesVisit;

        public MctsNode parent;
        public List<MctsNode> childs;

        public float AverageScore
        {
            get
            {
                if (timesVisit == 0)
                    return 0;
                else
                    return score / timesVisit;
            }
        }

        public void SetParent(MctsNode parent)
        {
            this.parent = parent;
        }

        public void ClearNode()
        {
            score = 0;
            timesVisit = 0;
            if (childs != null)
                childs.Clear();

            parent = null;
        }

        public MctsNode AddChild(int[] registeredMove, float score = 0f, int visit = 0)
        {
            MctsNode node = new MctsNode();
            node.registeredMove = registeredMove;
            node.score = score;
            node.timesVisit = visit;

            node.SetParent(this);

            if (childs == null)
                childs = new List<MctsNode>();

            childs.Add(node);
            return node;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            MctsNode node = obj as MctsNode;

            if (registeredMove == null || node.registeredMove == null 
                || registeredMove.Length != node.registeredMove.Length)
            {
                return false;
            }
            
            for (int i = 0; i < registeredMove.Length; i++)
            {
                if (registeredMove[i] != node.registeredMove[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return registeredMove.GetHashCode();
        }

        public override string ToString()
        {
            if (registeredMove == null)
                registeredMove = new int[3];
            return $"[{string.Join(", ", registeredMove)}]";
        }
    }

    
}
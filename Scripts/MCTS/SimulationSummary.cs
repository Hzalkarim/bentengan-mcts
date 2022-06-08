using System.Text;

namespace Bentengan.Mcts
{
    public class SimulationSummary
    {
        public string teamName;
        public int winCount;
        public int loseCount;
        public int captureOpponentCount;
        public int capturedCount;
        public int rescueCount;
        public int jailBreakCount;

        public void AddCount(GameplayHighlight highlight)
        {
            switch (highlight)
            {
                case GameplayHighlight.GameWon:
                    winCount++;
                    break;
                case GameplayHighlight.GameDraw:
                    //loseCount++;
                    break;
                case GameplayHighlight.PersonCaptured:
                    capturedCount++;
                    break;
                case GameplayHighlight.PersonRescued:
                    rescueCount++;
                    break;
            }
        }

        public void AddCount(GameplayHighlight highlight, string teamName)
        {
            if (!this.teamName.Equals(teamName))
            {
                if (highlight == GameplayHighlight.PersonCaptured)
                    captureOpponentCount++;
                else if (highlight == GameplayHighlight.GameWon)
                    loseCount++;
                else if (highlight == GameplayHighlight.PersonRescued)
                    jailBreakCount++;
                return;
            }

            AddCount(highlight);
        }

        public void ResetCount()
        {
            winCount = 0;
            loseCount = 0;
            captureOpponentCount = 0;
            capturedCount = 0;
            rescueCount = 0;
            jailBreakCount = 0;
        }

        public override string ToString()
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("=======================\n");
            strBuilder.Append($"Team: {teamName}\n");
            strBuilder.Append($"WIN: {winCount}\n");
            strBuilder.Append($"LOSE: {loseCount}\n");
            strBuilder.Append($"Capturing: {captureOpponentCount}\n");
            strBuilder.Append($"Captured: {capturedCount}\n");
            strBuilder.Append($"Rescue: {rescueCount}\n");
            strBuilder.Append($"Jailbreak: {jailBreakCount}\n");
            strBuilder.Append($"=======================");
            return strBuilder.ToString();
        }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Contracts
{
    public class GameStats
    {
        public long TotalGames { get; set; }

        public long XWins { get; set; }

        public long OWins { get; set; }

        public long Draws { get; set; }

        public long FailedGames { get; set; }


        public long FailedJoins { get; set; }

        public string? Reason { get; set; }

        public string Display()
        {
            return $"TotalGames: {TotalGames}, XWins: {XWins}, OWins: {OWins}, Draws: {Draws}, FailedGames: {FailedGames}, FailedJoins: {FailedJoins}, Reason: {Reason}";
        }

        public void Append(GameStats gs)
        {
            TotalGames += gs.TotalGames;
            XWins += gs.XWins;
            OWins += gs.OWins;
            Draws += gs.Draws;
            FailedGames += gs.FailedGames;
            FailedJoins += gs.FailedJoins;
            Reason += gs.Reason ?? string.Empty;
        }
    }
}

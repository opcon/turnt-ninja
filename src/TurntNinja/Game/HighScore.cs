using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurntNinja.Game
{
    public class HighScoreEntry
    {
        public ObjectId Id { get; set; }
        public string SongName { get; set; }
        public long SongID { get; set; }
        public List<PlayerScore> HighScores { get; set; }
        public DifficultyLevels Difficulty { get; set; }

        public HighScoreEntry()
        {
            HighScores = new List<PlayerScore>();
        }
    }

    public class PlayerScore
    {
        public string Name { get; set; }
        public long Score { get; set; }
        public float Accuracy { get; set; }
    }
}

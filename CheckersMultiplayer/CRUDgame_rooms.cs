using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersMultiplayer
{
    class CRUDgame_rooms
    {
        public string host { get; set; }
        public string blackPawns { get; set; }
        public string whitePawns { get; set; }
        public string roomName { get; set; }
        public string password { get; set; }
        public List<List<string>> board { get; set; }
        public bool inProgress { get; set; }
        public string turn {  get; set; }
    }
}

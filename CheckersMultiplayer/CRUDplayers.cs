using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersMultiplayer
{
    class CRUDplayers
    {
        //datas for database
        public string name { get; set; }
        public string login { get; set; }
        public string email { get; set; }
        public int age { get; set; }
        public bool online { get; set; }
        public bool inGame { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FireSharp.Response;
using Newtonsoft.Json;

namespace CheckersMultiplayer
{
    class CRUD_Service
    {
        CRUD_Connection conn = new CRUD_Connection();

        //set datas to database
        public void SetData(string name, string login, string password, int age, bool online, bool inGame)
        {
            try
            {
                CRUDplayers set = new CRUDplayers()
                {
                    name = name,
                    login = login,
                    password = password,
                    age = age,
                    online = online,
                    inGame = inGame
                };
                var SetData = conn.client.Set("players/" + login, set);
            }
            catch (Exception)
            {
                Console.WriteLine("error");
            }

        }

        //Update datas
        public void UpdateData(string name, string login, string password, int age, bool online, bool inGame)
        {
            try
            {
                CRUDplayers set = new CRUDplayers()
                {
                    name = name,
                    login = login,
                    password = password,
                    age = age,
                    online = online,
                    inGame = inGame
                };
                var SetData = conn.client.Update("players/" + login, set); ;
            }
            catch (Exception)
            {
                Console.WriteLine("error");
            }
        }

        //Delete datas
        public void DeleteData(string name, string login, string password, int age, bool online, bool inGame)
        {
            try
            {
                var SetData = conn.client.Delete("players/" + login);
            }
            catch (Exception)
            {
                Console.WriteLine("error");
            }
        }

        //List of the datas
        public Dictionary<string, CRUDplayers> LoadData()
        {
            try
            {
                FirebaseResponse al = conn.client.Get("players");
                Dictionary<string, CRUDplayers> ListData = JsonConvert.DeserializeObject<Dictionary<string, CRUDplayers>>(al.Body.ToString());
                return ListData;
            }
            catch (Exception)
            {
                Console.WriteLine("error");
                return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Firebase.Auth;
using FireSharp.Response;
using Newtonsoft.Json;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System.Net.Http;
using Firebase.Functions;
using System.Drawing.Drawing2D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.ComponentModel;

namespace CheckersMultiplayer
{
    class CRUD_Service
    {
        CRUD_Connection conn = new CRUD_Connection();

        public bool RegisterUser(string email,string password)
        {
            try
            {
                var registerTask = conn.authClient.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                    if (task.IsCanceled)
                    {
                        Console.WriteLine("CreateUserWithEmailAndPasswordAsync was canceled.");
                    }
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    }

                    // Firebase user has been created.
                    UserCredential result = task.Result;
                    Console.WriteLine("Firebase user created successfully:" +result.User.Uid);
                });

                registerTask.Wait();
                return true;
            }
            catch(HttpRequestException)
            {
                return false;
            }
            catch(FirebaseAuthHttpException)
            {
                return false;
            }
            catch (AggregateException)
            {
                return false;
            }
        }


        public bool LoginUser(string email, string password)
        {
            try
            {
                var loginTask = conn.authClient.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                    if (task.IsCanceled)
                    {
                        Console.WriteLine("SignInWithEmailAndPasswordAsync was canceled.");
                    }
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    }

                    UserCredential result = task.Result;

                    Console.WriteLine("User signed in successfully: " + result.User.Uid);
                });

                loginTask.Wait();
                return true;
            }
            catch(HttpRequestException)
            {
                return false;
            }
            catch(FirebaseAuthHttpException)
            {
                return false;
            }
            catch (AggregateException)
            {
                return false;
            }
        }

        public Firebase.Auth.User LogoutUser()
        {
            conn.authClient.SignOut();

            return conn.authClient.User;
        }

        //set datas to database
        public void SetData(string name, string login, string email, int age, bool online, bool inGame)
        {
            try
            {
                CRUDplayers set = new CRUDplayers()
                {
                    name = name,
                    login = login,
                    email=email,
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

        public CRUDgame_rooms CreateGameRoom(string login)
        {
            try
            {
                CRUDgame_rooms set = new CRUDgame_rooms()
                {
                    host = login,
                    blackPawns = login,
                    whitePawns = "",
                    password = "",
                    roomName= login+"'s room",
                    board = new List<List<string>>
                    {
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "B", "0", "B", "0", "B", "0", "B", "0" },
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" },
                        new List<string> { "0", "W", "0", "W", "0", "W", "0", "W" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" }
                    },
                    turn = login
                };
                var SetData = conn.client.Set("gameRooms/" + login, set);

                return set;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public void UpdateGameRoom(string login,string password, string roomName,string whitePawns, bool inProgress)
        {
            try
            {
                CRUDgame_rooms set = new CRUDgame_rooms()
                {
                    host = login,
                    blackPawns = login,
                    whitePawns = whitePawns,
                    password = password,
                    roomName = roomName,
                    board = new List<List<string>>
                    {
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "B", "0", "B", "0", "B", "0", "B", "0" },
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" },
                        new List<string> { "0", "W", "0", "W", "0", "W", "0", "W" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" }
                    },
                    inProgress=inProgress,
                    turn = login
                };
                var SetData = conn.client.Update("gameRooms/" + login, set);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void UpdateGameRoomBoard(string login, List<List<string>> newBoard)
        {
            try
            {
                var updateData = conn.client.Update("gameRooms/" + login, new
                {
                    board = newBoard
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void UpdateGameRoomTurn(string login, string newTurn)
        {
            try
            {
                var updateData = conn.client.Update("gameRooms/" + login, new
                {
                    turn = newTurn
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void ClearGameRoomWhitePawns(string host,string blackPawns, string password, string roomName, List<List<string>> board, string turn)
        {
            try
            {
                var gameRoomExists = conn.client.Get("gameRooms/" + host);

                CRUDgame_rooms set = new CRUDgame_rooms()
                {
                    host = host,
                    blackPawns = blackPawns,
                    whitePawns = "",
                    password = password,
                    roomName = roomName,
                    board = board,
                    turn = turn
                };
                var SetData = conn.client.Update("gameRooms/" + host, set);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }


        public void UpdateGameRoomOpponent(string login, string password, string roomName,string opponent)
        {
            try
            {
                CRUDgame_rooms set = new CRUDgame_rooms()
                {
                    host = login,
                    blackPawns = login,
                    whitePawns = opponent,
                    password = password,
                    roomName = roomName,
                    board = new List<List<string>>
                    {
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "B", "0", "B", "0", "B", "0", "B", "0" },
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" },
                        new List<string> { "0", "W", "0", "W", "0", "W", "0", "W" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" }
                    },
                    turn = login
                };
                var SetData = conn.client.Update("gameRooms/" + login, set);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void DeleteGameRoom(string accountLogin)
        {
            try
            {
                var SetData = conn.client.Delete("gameRooms/" + accountLogin);
            }
            catch (Exception)
            {
                Console.WriteLine("error");
            }
        }

        //Update datas
        public void UpdateData(string name, string login, string email, int age, bool online, bool inGame)
        {
            try
            {
                CRUDplayers set = new CRUDplayers()
                {
                    name = name,
                    login = login,
                    email = email,
                    age = age,
                    online = online,
                    inGame = inGame
                };
                var SetData = conn.client.Update("players/" + login, set);
            }
            catch (Exception)
            {
                Console.WriteLine("error");
            }
        }

        //Delete datas
        public void DeleteData(string name, string login, string email, int age, bool online, bool inGame)
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
        public Dictionary<string, CRUDplayers> LoadPlayers()
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

        public Dictionary<string, CRUDgame_rooms> LoadGameRooms()
        {
            try
            {
                FirebaseResponse al = conn.client.Get("gameRooms");
                string json = al.Body.ToString();

                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                Dictionary<string, CRUDgame_rooms> gameRooms = JsonConvert.DeserializeObject<Dictionary<string, CRUDgame_rooms>>(json, settings);

                return gameRooms;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }


    }
}

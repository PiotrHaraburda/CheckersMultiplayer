using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Newtonsoft.Json;

namespace CheckersMultiplayer
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

            for (int i = 10; i < 100; i++)
                ageComboBox.Items.Add(i);

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void onRegisterClick(object sender, RoutedEventArgs e)
        {
            bool alreadyRegistered = false;
            Crud crud = new Crud();

            foreach (var item in crud.LoadData())
            {
                if (item.Value.login == loginTextBox.Text)
                    alreadyRegistered = true;
            }

            if (alreadyRegistered == false)
                crud.SetData(nameTextBox.Text, loginTextBox.Text, passwordTextBox.Password, Int32.Parse(ageComboBox.Text), false, false);
            else
                loginTextBox.Text = "This login is already taken!";
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if(loginTextBox.Text == "This login is already taken!")
                loginTextBox.Foreground = Brushes.Red;
            else
                loginTextBox.Foreground = Brushes.Black;
        }
    }

    class connection
    {
        //firebase connection Settings
        public IFirebaseConfig fc = new FirebaseConfig()
        {
            AuthSecret = "kD2SaWj1hzgdv5HPdVLUwNDIzyDUpRsWCtNoqZWh",
            BasePath = "https://checkersmultiplayer-ea56b-default-rtdb.europe-west1.firebasedatabase.app/"
        };

        public IFirebaseClient client;
        //Code to warn console if class cannot connect when called.
        public connection()
        {
            try
            {
                client = new FireSharp.FirebaseClient(fc);
            }
            catch (Exception)
            {
                Console.WriteLine("database connection error");
            }
        }
    }

    class data
    {
        //datas for database
        public string name { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public int age { get; set; }
        public bool online { get; set; }
        public bool inGame { get; set; }
    }

    class Crud
    {
        connection conn = new connection();

        //set datas to database
        public void SetData(string name, string login, string password, int age, bool online, bool inGame)
        {
            try
            {
                data set = new data()
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
                data set = new data()
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
        public Dictionary<string, data> LoadData()
        {
            try
            {
                FirebaseResponse al = conn.client.Get("players");
                Dictionary<string, data> ListData = JsonConvert.DeserializeObject<Dictionary<string, data>>(al.Body.ToString());
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

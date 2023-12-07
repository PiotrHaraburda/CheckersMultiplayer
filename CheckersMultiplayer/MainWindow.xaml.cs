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
            Crud crud = new Crud();
            crud.SetData("Jan", "Kowalski", 14);
            crud.SetData("Marcin", "Nowak", 19);
            crud.SetData("Pawel", "Kwiatkowski", 24);
            crud.SetData("Jerzy", "Wozniak", 44);
            foreach (var item in crud.LoadData())
            {
                Console.WriteLine("Name :" + item.Value.Name);
                Console.WriteLine("Surname :" + item.Value.Surname);
                Console.WriteLine("age :" + item.Value.age);
            }
            crud.DeleteData("Jan");
            crud.UpdateData("Jerzy", "Wozniak", 35);
            Console.WriteLine("\nUpdated Data\n\n");
            foreach (var item in crud.LoadData())
            {
                Console.WriteLine("Name :" + item.Value.Name);
                Console.WriteLine("Surname :" + item.Value.Surname);
                Console.WriteLine("age :" + item.Value.age);
            }
            Console.ReadLine();
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
        public string Name { get; set; }
        public string Surname { get; set; }
        public int age { get; set; }
    }

    class Crud
    {
        connection conn = new connection();

        //set datas to database
        public void SetData(string Name, string Surname, int age)
        {
            try
            {
                data set = new data()
                {
                    Name = Name,
                    Surname = Surname,
                    age = age
                };
                var SetData = conn.client.Set("people/" + Name, set);
            }
            catch (Exception)
            {
                Console.WriteLine("error");
            }

        }

        //Update datas
        public void UpdateData(string Name, string Surname, int age)
        {
            try
            {
                data set = new data()
                {
                    Name = Name,
                    Surname = Surname,
                    age = age
                };
                var SetData = conn.client.Update("people/" + Name, set); ;
            }
            catch (Exception)
            {
                Console.WriteLine("error");
            }
        }

        //Delete datas
        public void DeleteData(string Name)
        {
            try
            {
                var SetData = conn.client.Delete("people/" + Name);
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
                FirebaseResponse al = conn.client.Get("people");
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

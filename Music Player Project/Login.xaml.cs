using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Music_Player_Project
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        //================================================ Variables ================================================//
        private const string FILE_NAME = "accounts.dat";
        public List<Account> accounts = new List<Account>();

        //================================================ Startup ================================================//

        //Load Account data upon Startup
        private async void Window_Initialized(object sender, EventArgs e)
        {
            //create formatter
            BinaryFormatter formatter = new BinaryFormatter();

            //Try to load accounts
            try
            {
                //Create filestream
                FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read);
                //Deserialize file
                accounts = await Task.Run(() => (List<Account>)formatter.Deserialize(fs));
                //Dispose of filestream
                fs.Dispose();
            }
            catch //Catch Filenotfound exception
            {
                //Alert user to error
                MessageBox.Show("Error in loading accounts, a new account file has been created. Please restart program!", "Account load Error");
                //Add default accounts
                accounts.Add(new Account("Admin", "Password"));
                accounts.Add(new Account("Guest", "Password"));
                accounts.Add(new Account("Dragon Loli", "ravioli ravioli dont lewd the dragon loli"));
                //Create new filestream
                FileStream fs = new FileStream(FILE_NAME, FileMode.Create, FileAccess.Write);
                //Use empty list to create new file
                await Task.Run(() =>formatter.Serialize(fs, accounts));
                //Dispose of filestream
                fs.Dispose();
            }
        }

        //================================================ Event Handlers ================================================//

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            //Gather Credentials
            string userName = txtLoginUserName.Text;
            string password = txtLoginPassword.Password;
            //Check for null or white space
            if (!String.IsNullOrWhiteSpace(userName) && !String.IsNullOrWhiteSpace(password))
            {
                //Find matching account
                Account temp = await Task.Run(() => accounts.Find(Account => Account.UserName == userName));
                //Check for valid account
                if (temp != null)
                {
                    //Hash password with found account's salt
                    var saltBytes = Convert.FromBase64String(temp.Salt);
                    //Prepare hasher
                    var hasher = new Rfc2898DeriveBytes(password, saltBytes, 100);
                    //Get Hash
                    var Passkey = Convert.ToBase64String(hasher.GetBytes(256));
                    hasher.Dispose();
                    //Passkey check
                    if (Passkey.CompareTo(temp.Passkey) == 0)
                    {
                        MainWindow main = new MainWindow(temp);
                        main.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Login Details were Invalid");
                    }
                }
                else
                {
                    MessageBox.Show("The username does not match any accounts that exist!", "Account does not Exist!");
                }
            }
        }

        private async void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            //Variables
            string userName = txtNewUserName.Text;
            string password1 = txtNewPassword1.Password;
            string password2 = txtNewPassword2.Password;

            //Verification Check
            if (password1.CompareTo(password2) == 0 && !String.IsNullOrWhiteSpace(userName))
            {
                await Task.Run(() => accounts.Add(new Account(userName, password1)));
                MessageBox.Show("Account creation was successful!", "Account Creation Complete");
                txtNewUserName.Clear();
                txtNewPassword1.Clear();
                txtNewPassword2.Clear();

                //create formatter
                BinaryFormatter formatter = new BinaryFormatter();
                //Create new filestream
                FileStream fs = new FileStream(FILE_NAME, FileMode.Create, FileAccess.Write);
                //Save new account
                await Task.Run(() => formatter.Serialize(fs, accounts));
                //Dispose of filestream
                fs.Dispose();
            }
            else
            {
                MessageBox.Show("Passwords do not match or invalid username", "Error");
                txtNewPassword1.Clear();
                txtNewPassword2.Clear();
            }

        }

        private async void BtnLoginAsGuest_Click(object sender, RoutedEventArgs e)
        {
            //Find Guest account
            Account temp = await Task.Run(() => accounts.Find(Account => Account.UserName == "Guest"));
            MainWindow main = new MainWindow(temp);
            main.Show();
            this.Close();
        }
    }
}

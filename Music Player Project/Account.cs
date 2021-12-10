using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Music_Player_Project
{

    [Serializable]
    public class Account
    {

        //Variables
        public string UserName { get; private set; }
        public string Passkey { get; private set; }
        public string Salt { get; private set; }


        /// <summary>
        /// Constructor for making new account
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public Account(string userName, string password)
        {
            //================== Prepare ==================//

            //Set username
            this.UserName = userName;

            //prepare byte array
            byte[] saltBytes = new byte[128];

            //================== Salting ==================//

            //Salt password
            var salter = new RNGCryptoServiceProvider();
            //Generate salt
            salter.GetNonZeroBytes(saltBytes);
            salter.Dispose();
            //Save Salt
            this.Salt = Convert.ToBase64String(saltBytes);

            //================== Hashing ==================//

            //Prepare hasher
            var hasher = new Rfc2898DeriveBytes(password, saltBytes, 100);
            //Get Hash
            this.Passkey = Convert.ToBase64String(hasher.GetBytes(256));
            hasher.Dispose();
        }
    }
}

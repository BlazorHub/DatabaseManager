﻿using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class Common
    {
        public static string GetConnectionString(ConnectParameters connection)
        {
            string source = $"Source={connection.DatabaseServer};";
            string database = $"Initial Catalog ={connection.Database};";
            string timeout = "Connection Timeout=120";
            string persistSecurity = "Persist Security Info=False;";
            string multipleActive = "MultipleActiveResultSets=True;";
            string integratedSecurity = "";
            string user = "";
            //Encryption is currently not used, more testing later
            string encryption = "Encrypt=True;TrustServerCertificate=False;";
            if (!string.IsNullOrWhiteSpace(connection.DatabaseUser))
                user = $"User ID={connection.DatabaseUser};";
            else
                integratedSecurity = "Integrated Security=True;";
            string password = "";
            if (!string.IsNullOrWhiteSpace(connection.DatabasePassword)) password = $"Password={connection.DatabasePassword};";

            string cnStr = "Data " + source + persistSecurity + database +
                user + password + integratedSecurity + multipleActive;

            cnStr = cnStr + timeout;

            return cnStr;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseManager.Shared
{
    public class ConnectParameters
    {
        [Required]
        public string SourceName { get; set; }
        [Required]
        public string Database { get; set; }
        [Required]
        public string DatabaseServer { get; set; }
        public string DatabaseUser { get; set; }
        public string DatabasePassword { get; set; }
        public string  ConnectionString { get; set; }
    }
}

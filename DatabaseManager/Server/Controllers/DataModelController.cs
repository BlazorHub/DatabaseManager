﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.AspNetCore.Hosting;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataModelController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly string connectionString;
        private readonly string container = "sources";

        public DataModelController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _env = env;
        }

        [HttpPost]
        public async Task<ActionResult<string>> DataModelCreate(DataModelParameters dmParameters)
        {
            if (dmParameters == null) return BadRequest();

            try
            {
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, dmParameters.DataConnector);
                if (connector == null) return BadRequest();
                if (dmParameters.ModelOption == "PPDM Model")
                {
                    CreatePPDMModel(dmParameters, connector);
                }
                else if (dmParameters.ModelOption == "DMS Model")
                {
                    CreateDMSModel(connector);
                }
                else if (dmParameters.ModelOption == "Stored Procedures")
                {
                    CreateStoredProcedures(connector);
                }
                else if (dmParameters.ModelOption == "PPDM Modifications")
                {
                    CreatePpdmModifications(connector);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        private void CreateStoredProcedures(ConnectParameters connector)
        {
            try
            {
                string contentRootPath = _env.ContentRootPath;
                string sqlFile = contentRootPath + @"\DataBase\StoredProcedures.sql";
                string sql = System.IO.File.ReadAllText(sqlFile);
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        dbConn.SQLExecute(commandText[x]);
                    }
                }
                
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private void CreateDMSModel(ConnectParameters connector)
        {
            try
            {
                string contentRootPath = _env.ContentRootPath;
                string sqlFile = contentRootPath + @"\DataBase\DataScienceManagement.sql";
                string sql = System.IO.File.ReadAllText(sqlFile);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                dbConn.SQLExecute(sql);
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private void CreatePpdmModifications(ConnectParameters connector)
        {
            try
            {
                string contentRootPath = _env.ContentRootPath;
                string sqlFile = contentRootPath + @"\DataBase\PpdmModifications.sql";
                string sql = System.IO.File.ReadAllText(sqlFile);
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        dbConn.SQLExecute(commandText[x]);
                    }
                }
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private void CreatePPDMModel(DataModelParameters dmParameters, ConnectParameters connector)
        {
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient fileClient = account.CreateCloudFileClient();
                CloudFileShare share = fileClient.GetShareReference(dmParameters.FileShare);
                if (share.Exists())
                {
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                    CloudFile file = rootDir.GetFileReference(dmParameters.FileName);
                    if (file.Exists())
                    {
                        string sql = file.DownloadTextAsync().Result;
                        DbUtilities dbConn = new DbUtilities();
                        dbConn.OpenConnection(connector);
                        dbConn.SQLExecute(sql);
                        dbConn.CloseConnection();
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create PPDM Model Error: ", ex);
                throw error;
            }
        }
    }
}
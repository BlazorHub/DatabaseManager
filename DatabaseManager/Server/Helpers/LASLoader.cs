﻿using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class LASLoader
    {
        private DbUtilities _dbConn;
        private string _uwi;
        private string _nullRepresentation;
        private List<string> _logNames = new List<string>();
        private List<double> _curveValues = new List<double>();
        private List<double> _indexValues = new List<double>();
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();

        public LASLoader(IWebHostEnvironment env)
        {
            _dbConn = new DbUtilities();
            _nullRepresentation = "-999.25";

            try
            {
                string contentRootPath = env.ContentRootPath;
                string jsonFile = contentRootPath + @"\DataBase\ReferenceTables.json";
                string json = System.IO.File.ReadAllText(jsonFile);
                _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(json);
                jsonFile = contentRootPath + @"\DataBase\PPDMDataAccess.json";
                json = System.IO.File.ReadAllText(jsonFile);
                _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(json);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Read refernce table file error: ", ex);
                throw error;
            }
        }

        public void LoadLASFile(ConnectParameters connector, string fileText)
        {
            string versionInfo = "";
            string wellInfo = "";
            string curveInfo = "";
            string parameterInfo = "";
            string dataInfo = "";
            string[] sections = fileText.Split("~", StringSplitOptions.RemoveEmptyEntries);
            foreach (string section in sections)
            {
                string flag = section.Substring(0, 1);
                if (flag == "V") versionInfo = section;
                if (flag == "W") wellInfo = section;
                if (flag == "C") curveInfo = section;
                if (flag == "P") parameterInfo = section;
                if (flag == "A") dataInfo = section;
            }

            _dbConn.OpenConnection(connector);

            GetVersionInfo(versionInfo);
            GetHeaderInfo(wellInfo);
            GetCurveInfo(curveInfo);
            GetDataInfo(dataInfo);
            LoadLogs();

            _dbConn.CloseConnection();
        }

        private void LoadLogs()
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            int logCount = _logNames.Count();
            GetIndexValues();
            for (int k = 1; k < logCount; k++)
            {
                Dictionary<string, string> logHeader = new Dictionary<string, string>();
                string[] attributes = Common.GetAttributes(dataType.Select);
                foreach (string attribute in attributes)
                {
                    logHeader.Add(attribute.Trim(), "");
                }
                logHeader["NULL_REPRESENTATION"] = _nullRepresentation;
                logHeader["VALUE_COUNT"] = "99999.0";
                logHeader["MAX_INDEX"] = "99999.0";
                logHeader["MIN_INDEX"] = "99999.0";
                logHeader["UWI"] = _uwi;
                string logName = Common.FixAposInStrings(_logNames[k]);
                logHeader["CURVE_ID"] = logName;
                string json = JsonConvert.SerializeObject(logHeader, Formatting.Indented);
                LoadLogHeader(json, logName, k);
            }
        }

        private void GetIndexValues()
        {
            int logCount = _logNames.Count();
            int valueCount = _curveValues.Count() / logCount;
            int index = 0;
            for (int m = 0; m < valueCount; m++)
            {
                index = m * logCount;
                _indexValues.Add(_curveValues[index]);
            }
        }

        private void LoadLogHeader(string json, string logName, int pointer)
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            string select = dataType.Select;
            string query = $" where UWI = '{_uwi}' and CURVE_ID = '{logName}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
            {
                json = Common.SetJsonDataObjectDate(json, "ROW_CREATED_DATE");
                json = Common.SetJsonDataObjectDate(json, "ROW_CHANGED_DATE");
                _dbConn.InsertDataObject(json, "Log");
                LoadLogCurve(pointer, logName);
            }
        }

        private void LoadLogCurve(int pointer, string logName)
        {
            string comma = "";
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "LogCurve");
            int indexCount = _indexValues.Count();
            int logCount = _logNames.Count();
            string jsonArray = @"[";
            for (int i = 0; i < indexCount; i++)
            {
                jsonArray = jsonArray + comma;
                Dictionary<string, string> logCurve = new Dictionary<string, string>();
                string[] attributes = Common.GetAttributes(dataType.Select);
                foreach (string attribute in attributes)
                {
                    logCurve.Add(attribute.Trim(), "");
                }
                logCurve["UWI"] = _uwi;
                logCurve["CURVE_ID"] = logName;
                logCurve["SAMPLE_ID"] = i.ToString();
                logCurve["INDEX_VALUE"] = _indexValues[i].ToString();
                logCurve["MEASURED_VALUE"] = _curveValues[pointer + (i * logCount)].ToString();
                string json = JsonConvert.SerializeObject(logCurve, Formatting.Indented);
                json = Common.SetJsonDataObjectDate(json, "ROW_CREATED_DATE");
                json = Common.SetJsonDataObjectDate(json, "ROW_CHANGED_DATE");
                jsonArray = jsonArray + json;
                comma = ",";
            }
            jsonArray = jsonArray + @"]";
            string select = dataType.Select;
            string query = $" where UWI = '{_uwi}' and CURVE_ID = '{logName}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
            {
                _dbConn.InsertDataObject(jsonArray, "LogCurve");
            }
        }


        private void GetDataInfo(string dataInfo)
        {
            string input = null;
            double value;
            StringReader sr = new StringReader(dataInfo);
            input = sr.ReadLine();
            while ((input = sr.ReadLine()) != null)
            {
                string cValues = input;
                for (int j = 0; j < _logNames.Count; j++)
                {
                    cValues = cValues.Trim();
                    int space = cValues.IndexOf(" ");
                    string cValue = string.Empty;
                    if (space > -1)
                    {
                        cValue = cValues.Substring(0, space);
                    }
                    else
                    {
                        cValue = cValues;
                    }
                    bool canConvert = double.TryParse(cValue, out value);
                    if (!canConvert)
                    {
                        value = -999.25;
                    }
                    _curveValues.Add(value);
                    if (space > -1) cValues = cValues.Substring(space);
                }
            }
        }

        private void GetCurveInfo(string curveInfo)
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            Dictionary<string, string> log = new Dictionary<string, string>();
            string[] attributes = Common.GetAttributes(dataType.Select);
            foreach (string attribute in attributes)
            {
                log.Add(attribute.Trim(), "");
            }

            string input = null;
            StringReader sr = new StringReader(curveInfo);
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (!string.IsNullOrEmpty(line.Mnem))
                {
                    log["CURVE_ID"] = line.Mnem.Trim();
                    _logNames.Add(line.Mnem.Trim());
                    log["UWI"] = _uwi;
                    var json = JsonConvert.SerializeObject(log, Formatting.Indented);
                }
            }
        }

        
        private void GetHeaderInfo(string wellInfo)
        {
            LASHeaderMappings headMap = new LASHeaderMappings();
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string input = null;
            Dictionary<string, string> header = new Dictionary<string, string>();
            string[] attributes = Common.GetAttributes(dataType.Select);
            foreach (string attribute in attributes)
            {
                header.Add(attribute.Trim(), "");
            }
            header["FINAL_TD"] = "99999.0";
            header["SURFACE_LATITUDE"] = "99999.0";
            header["SURFACE_LONGITUDE"] = "99999.0";
            header["DEPTH_DATUM_ELEV"] = "99999.0";
            header["GROUND_ELEV"] = "99999.0";
            header["ASSIGNED_FIELD"] = "UNKNOWN";
            header["OPERATOR"] = "UNKNOWN";
            header["DEPTH_DATUM"] = "UNKNOWN";
            header["CURRENT_STATUS"] = "UNKNOWN";
            header.Add("API", "");
            StringReader sr = new StringReader(wellInfo);
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (!string.IsNullOrEmpty(line.Mnem))
                {
                    try
                    {
                        string key = headMap[line.Mnem];
                        header[key] = line.Data;
                        if (key == "NULL") _nullRepresentation = line.Data;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            if (string.IsNullOrEmpty(header["UWI"])) header["UWI"] = header["API"];
            if (string.IsNullOrEmpty(header["UWI"])) header["UWI"] = header["LEASE_NAME"] + "-" + header["WELL_NAME"];
            string json = JsonConvert.SerializeObject(header, Formatting.Indented);
            _uwi = header["UWI"];
            LoadHeader(json);
        }

        private void LoadHeader(string json)
        {
            foreach(ReferenceTable reference in _references)
            {
                json = CheckHeaderForeignKeys(json, reference);
            }
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string select = dataType.Select;
            string query = $" where UWI = '{_uwi}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
            {
                json = Common.SetJsonDataObjectDate(json, "ROW_CREATED_DATE");
                json = Common.SetJsonDataObjectDate(json, "ROW_CHANGED_DATE");
                _dbConn.InsertDataObject(json, "WellBore");
            }
        }

        private string CheckHeaderForeignKeys(string json, ReferenceTable reference)
        {
            
            JObject dataObject = JObject.Parse(json);
            string field = dataObject[reference.ReferenceAttribute].ToString();
            field = Common.FixAposInStrings(field);
            string select = $"Select * from {reference.Table} ";
            string query = $" where {reference.KeyAttribute} = '{field}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
            {
                if (reference.Insert)
                {
                    string strInsert = $"insert into {reference.Table} ";
                    string strValue = $" ({reference.KeyAttribute}, {reference.ValueAttribute}) values ('{field}', '{field}')";
                    string strQuery = "";
                    _dbConn.DBInsert(strInsert, strValue, strQuery);
                }
                else
                {
                    dataObject[reference.ReferenceAttribute] = "UNKNOWN";
                }
            }
            string newJson = json;
            return newJson;
        }

        private void GetVersionInfo(string versionInfo)
        {
            string input = null;
            bool versionFound = false;

            StringReader sr = new StringReader(versionInfo);
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (line.Mnem == "VERS")
                {
                    if (line.Data.Substring(0,3) != "2.0")
                    {
                        throw new System.Exception("LAS file version not supported");
                    }
                    versionFound = true;
                }
                if (line.Mnem == "WRAP")
                {
                    if (line.Data != "NO")
                    {
                        throw new System.Exception("LAS file wrap is not supported");
                    }
                }
            }
            if (!versionFound)
            {
                throw new System.Exception("LAS file is missing the version tag");
            }
        }

        private static LASLine DecodeLASLine(string input)
        {
            LASLine line = new LASLine();
            string flag = input.Substring(0, 1);
            if (flag != "#")
            {
                int firstDot = input.IndexOf(".");
                if (firstDot > 0)
                {
                    line.Mnem = input.Substring(0, firstDot);
                    line.Mnem = line.Mnem.Trim();
                    input = input.Substring(firstDot + 1);
                    int firstSpace = input.IndexOf(" ");
                    string unit = string.Empty;
                    if (firstSpace > 0)
                    {
                        line.Unit = input.Substring(0, firstSpace);
                        input = input.Substring(firstSpace);
                    }
                    int lastColon = input.LastIndexOf(":");
                    if (lastColon > 0)
                    {
                        line.Data = input.Substring(0, lastColon);
                        line.Data = line.Data.Trim();
                        input = input.Substring(lastColon + 1);
                    }
                    line.Description = input;
                    line.Description = line.Description.Trim();
                    return line;
                }
            }

            return line;
        }
    }
}

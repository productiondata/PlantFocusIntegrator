using Newtonsoft.Json;
using PlantFocus_Integrator.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PlantFocus_Integrator
{
    class Program
    {
        private static DateTime fileExportDateTime;
        private static string fileExportName;
        private static PFIntegrator_Logger logger = new PFIntegrator_Logger();

        static async Task Main(string[] args)
        {
            try
            {
                const string requestIntegrationItemsURL = "http://localhost:5000/api/dor/E059BD02-1929-40B3-9387-FEECA93A648E/GetIntegrationItems?CustomerName=Davenport";
                const string postIntegrationDataURL = "http://localhost:5000/api/dor/E059BD02-1929-40B3-9387-FEECA93A648E/PostIntegrationData?CustomerName=Davenport";
                const string getIntegrationDataURL = "http://localhost:5000/api/dor/E059BD02-1929-40B3-9387-FEECA93A648E/GetIntegrationData?CustomerName=Davenport&DateToBePulled=";
                const string postIntegrationFlatFileInformation = "http://localhost:5000/api/dor/E059BD02-1929-40B3-9387-FEECA93A648E/PostFlatFileInformation?CustomerName=Davenport";
                const string postFlatFileIntegrationData = "http://localhost:5000/api/dor/E059BD02-1929-40B3-9387-FEECA93A648E/PostFlatFileIntegrationData?CustomerName=Davenport";

                if (args.Length > 0 && args[0].ToString() == "-import")
                {
                    ImportFlatFile(postFlatFileIntegrationData);
                }
                else if (args.Length > 0)
                {
                    logger.LogMessage_WriteLine($@"Unrecognized executable argument: {args[0].ToString()}");
                }
                else
                {
                    List<IntegrationItem> integrationItems = new List<IntegrationItem>();
                    List<IntegrationData> integrationData = new List<IntegrationData>();
                    List<IntegrationData> integrationDataToPost = new List<IntegrationData>();
                    List<IntegrationData> integrationDataToSend = new List<IntegrationData>();

                    //Call method that retrieves all integration items
                    logger.LogMessage_Write("Retrieving integration items...");
                    integrationItems = await GetIntegrationItems(requestIntegrationItemsURL);
                    if(integrationItems?.Count != null) logger.LogMessage_WriteLine($"{integrationItems.Count} retrieved integration items");
                    else logger.LogMessage_WriteLine($"0 retrieved integration items");


                    //Call method that builds all integration data that needs to be sent
                    logger.LogMessage_Write("Building integration data to be sent to queue...");
                    integrationData = await BuildIntegrationData(integrationItems);
                    if(integrationData?.Count != null) logger.LogMessage_WriteLine($" {integrationData.Count} integration data items to be sent to queue");
                    else logger.LogMessage_WriteLine($"0 integration data items to be sent to queue");

                    //Call method to post all integration data to be sent to database
                    logger.LogMessage_Write("Posting new integration data to queue...");
                    bool postDataSuccess = await PostIntegrationData(postIntegrationDataURL, integrationData);
                    if (postDataSuccess == true) logger.LogMessage_WriteLine(" SUCCESS", ConsoleColor.Green);
                    else { logger.LogMessage_WriteLine(" FAILURE, EXITING", ConsoleColor.Red); return; }

                    //Call method to retrieve all queue items to be sent
                    logger.LogMessage_Write("Retrieving all queue items...");
                    integrationDataToSend = await GetIntegrationDataToSend(getIntegrationDataURL);
                    logger.LogMessage_WriteLine($" {integrationDataToSend.Count} integration data items from queue to be outputted");

                    //Call method to output the flat file 
                    logger.LogMessage_Write("Outputing integration flat file...");
                    bool outputSuccess = OutputIngegrationFlatFile(integrationDataToSend);
                    if (outputSuccess == true) logger.LogMessage_WriteLine(" SUCCESS", ConsoleColor.Green);
                    else { logger.LogMessage_WriteLine(" FAILURE, EXITING"); return; }


                    //Call method to post flat file information of the data that exported
                    logger.LogMessage_Write("Updating database with flat file output information...");
                    bool postFlatFileInfoSuccess = await PostFlatFileInformation(postIntegrationFlatFileInformation, fileExportDateTime, fileExportName, integrationDataToSend);
                    if (outputSuccess == true) logger.LogMessage_WriteLine(" SUCCESS", ConsoleColor.Green);
                    else { logger.LogMessage_WriteLine(" FAILURE, EXITING", ConsoleColor.Red); return; }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Main");
            }
            finally
            {
                //Prevent console close
                Console.WriteLine("Press enter to close window.");
                Console.Read();
            }
        }

        static async void ImportFlatFile(string postURL)
        {
            try
            {
                const string importFileLocation = @"C:\Maximo Import\";
                const string expectedFileColumnHeaders = "FROMSTORELOC,SITEID,USETYPE,STATUS,LINETYPE,QUANTITY,ITEMNUM,FROMBIN,IUL_USETYPE,IUL_FROMSTORELOC,ACTUALDATE,IUL_ORGID";
                string[] directoryFiles = Directory.GetFiles(importFileLocation, "*.csv");
                List<IntegrationData> importData = new List<IntegrationData>();

                foreach (string file in directoryFiles)
                {
                    string[] fileContents = File.ReadAllLines(file);

                    if (fileContents[1].Equals(expectedFileColumnHeaders))
                    {
                        for (int i = 2; i < fileContents.Length; i++)
                        {
                            string[] values = fileContents[i].Split(',');
                            IntegrationData data = new IntegrationData();

                            data.FromStoreLoc = values[0];
                            data.SiteID = values[1];
                            data.UseType = values[2];
                            data.Status = values[3];
                            data.LineType = values[4];
                            data.Value = values[5];
                            data.ItemNum = values[6];
                            data.FromBin = values[7];
                            data.IUL_UseType = values[8];
                            data.IUL_FromStoreLoc = values[9];
                            data.dt = Convert.ToDateTime(values[10]);
                            data.IUL_OrgID = values[11];

                            importData.Add(data);
                        }
                        Console.WriteLine($"Sucessfully read and prepared data from {file} for import...");
                    }
                    else
                    {
                        Console.WriteLine($"Import header does not match expected import in {file}...");
                    }   
                }

                ApiHelper apiHelper = new ApiHelper();

                //If there is data items to send int he data object...
                if (importData.Count > 0)
                {
                    //...convert the list of integration data objects to json for http post to consume...
                    string json = JsonConvert.SerializeObject(importData);

                    //...and make the post call and return a boolean identifying if our data was successfully posted. 
                    await apiHelper.MakeHttpPostRequestAsync(postURL, json);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ImportFlatFile");
            }
        }

        static async Task<bool> PostFlatFileInformation(string postURL, DateTime exportDT, string fileName, List<IntegrationData> integrationData)
        {
            try
            {
                ApiHelper apiHelper = new ApiHelper();

                postURL = postURL + $@"&ExportDT={exportDT}&FileName={fileName}";

                //If there is data items to send int he data object...
                if (integrationData.Count > 0)
                {
                    //...convert the list of integration data objects to json for http post to consume...
                    string json = JsonConvert.SerializeObject(integrationData);

                    //...and make the post call and return a boolean identifying if our data was successfully posted. 
                    return await apiHelper.MakeHttpPostRequestAsync(postURL, json);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PostFlatFileInformation");
                return false;
            }
        }


        static async Task<List<IntegrationItem>> GetIntegrationItems(string getURL)
        {
            try
            {
                ApiHelper apiHelper = new ApiHelper();

                //Make HTTP call to API to retrieve a list of integration items that are eligible for update.
                string responseContent = await apiHelper.MakeHTTPGetRequestAsync(getURL);

                if(responseContent != null)
                {
                    return JsonConvert.DeserializeObject<List<IntegrationItem>>(responseContent);
                }
                else
                {
                    return null;
                }
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetIntegrationItems");
                return null;
            }
        }

        static bool OutputIngegrationFlatFile(List<IntegrationData> integrationData)
        {
            try
            {
                if (integrationData.Count > 0)
                {
                    DateTime exportTime = DateTime.Now;
                    string fileHeader = "PLANTFOCUS,CC_MATRECTRANS_PF,Add,EN\r\nFROMSTORELOC, SITEID, USETYPE, STATUS, LINETYPE, QUANTITY, ITEMNUM, FROMBIN, IUL_USETYPE, IUL_FROMSTORELOC, ACTUALDATE";
                    string filePath = $@"C:\Users\chiloh.edwards\Desktop\";
                    string fileName = $@"PFIntegrationFile_" + exportTime.ToString("yyyyMMdd_HHmmss") + ".dat";
                    string filePathName = filePath + fileName;

                    using (StreamWriter writer = new StreamWriter(filePathName, true))
                    {
                        writer.WriteLine(fileHeader);
                        foreach (IntegrationData data in integrationData)
                        {
                            writer.WriteLine($"{data.FromStoreLoc},{data.SiteID},{data.UseType},{data.Status},{data.LineType},{data.Value},{data.ItemNum},{data.FromBin},{data.IUL_UseType},{data.IUL_FromStoreLoc},{data.IUL_OrgID},{data.dt}");
                            data.SetFileID = fileName;
                        }

                        writer.Close();
                    }

                    fileExportDateTime = exportTime;
                    fileExportName = fileName;
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutputIngegrationFlatFile");
                return false;
            }
        }

        static async Task<List<IntegrationData>> GetIntegrationDataToSend(string getURL)
        {
            try
            {
                ApiHelper apiHelper = new ApiHelper();

                //Make HTTP call to API to retrieve a list of integration items that are eligible for update.
                string currentDate = DateTime.Now.Date.ToString("yyyy-MM-dd") + " 00:00:00";
                getURL = getURL + currentDate;
                string responseContent = await apiHelper.MakeHTTPGetRequestAsync(getURL);
                
                if(responseContent != null)
                {
                    return JsonConvert.DeserializeObject<List<IntegrationData>>(responseContent);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetIntegrationDataToSend");
                return null;
            }
        }

        static async Task<List<IntegrationData>> BuildIntegrationData(List<IntegrationItem> integrationItems)
        {
            try
            {
                List<IntegrationData> integrationData = new List<IntegrationData>();
                ApiHelper apiHelper = new ApiHelper();

                //Iterate over each integration item that we pulled in from the API
                foreach (IntegrationItem item in integrationItems)
                {
                    string itemURL = item.GetString;
                    string currentDate = DateTime.Now.Date.ToString("yyyy-MM-dd") + " 00:00:00";
                    string callResponse = "";

                    //Replace the GetString date with the current date that is being checked.
                    itemURL = itemURL.Replace("DateToBePulled", currentDate);

                    //Call for the current items sent data information
                    callResponse = await apiHelper.MakeHTTPGetRequestAsync(itemURL);

                    //If there is no response (no value that has been sent previous) or the last sent value is different then...
                    if (callResponse != "" && item.MostRecentSentValue != callResponse)
                    {
                        //...build our data item...
                        IntegrationData dataToSend = new IntegrationData();
                        dataToSend.dt = Convert.ToDateTime(currentDate);
                        dataToSend.iID = item.iID;
                        dataToSend.Value = callResponse;

                        //...and add it to the integrationData object.
                        integrationData.Add(dataToSend);
                    }
                }

                return integrationData;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BuildIntegrationData");
                return null;
            }

        }

        static async Task<bool> PostIntegrationData(string postURL, List<IntegrationData> integrationData)
        {
            try
            {
                ApiHelper apiHelper = new ApiHelper();

                //If there is data items to send int he data object...
                if (integrationData.Count > 0)
                {
                    //...convert the list of integration data objects to json for http post to consume...
                    string json = JsonConvert.SerializeObject(integrationData);

                    //...and make the post call and return a boolean identifying if our data was successfully posted. 
                    return await apiHelper.MakeHttpPostRequestAsync(postURL, json);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PostIntegrationData");
                return false;
            }
        }
    }
}

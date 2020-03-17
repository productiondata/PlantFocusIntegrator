using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlantFocus_Integrator
{
    public class PFIntegrator_Logger
    {
        private string filePath = "PFIntegratorLog_" + DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
        public void LogError(Exception ex, string methodName, ConsoleColor color = ConsoleColor.Red)
        {         
            int NumberOfRetries = 3;
            int DelayOnRetry = 1000;

            for (int i = 1; i <= NumberOfRetries; ++i)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine("-----------------------------------------------------------------------------");
                        writer.WriteLine("Date : " + DateTime.Now.ToString());

                        while (ex != null)
                        {
                            writer.WriteLine(ex.GetType().FullName);
                            writer.WriteLine("Message : " + ex.Message);
                            writer.WriteLine("StackTrace : " + ex.StackTrace);

                            ex = ex.InnerException;
                        }
                        writer.Close();
                    }

                    Console.ForegroundColor = color;
                    if(ex?.Message != null) Console.WriteLine($"Exception in {methodName}: " + ex.Message);
                    break;
                }
                catch (IOException e) when (i <= NumberOfRetries)
                {
                    Thread.Sleep(DelayOnRetry);
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        public void LogMessage_WriteLine(string message, ConsoleColor color = ConsoleColor.White)
        {
            int NumberOfRetries = 3;
            int DelayOnRetry = 1000;

            if (color != ConsoleColor.White) Console.ForegroundColor = color;
            else Console.ForegroundColor = color;

            for (int i = 1; i <= NumberOfRetries; ++i)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine("-----------------------------------------------------------------------------");
                        writer.WriteLine("Date : " + DateTime.Now.ToString());

                        if(message != null)
                        {
                            writer.WriteLine(message);
                        }
                        writer.Close();
                    }
                    Console.WriteLine(message);
                    break;
                }
                catch (IOException e) when (i <= NumberOfRetries)
                {
                    Thread.Sleep(DelayOnRetry);
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        public void LogMessage_Write(string message, ConsoleColor color = ConsoleColor.White)
        {
            int NumberOfRetries = 3;
            int DelayOnRetry = 1000;

            if (color != ConsoleColor.White) Console.ForegroundColor = color;
            else Console.ForegroundColor = color;

            for (int i = 1; i <= NumberOfRetries; ++i)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine("-----------------------------------------------------------------------------");
                        writer.WriteLine("Date : " + DateTime.Now.ToString() + ": " + message);
                        writer.Close();
                    }
                    Console.Write(message);
                    break;
                }
                catch (IOException e) when (i <= NumberOfRetries)
                {
                    Thread.Sleep(DelayOnRetry);
                }
            }

            Console.ForegroundColor = ConsoleColor.White;


        }
    }
}

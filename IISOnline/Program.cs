using Microsoft.Web.Administration;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
//using System.Xml.Linq;

internal class Program
{

    static void Main(string[] args)
    {
        
        List<(string Name, string State)> applicationPool = new List<(string Name, string State)>();

        QueryIIS(applicationPool);

        int autoRateMS = 5000;

        ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();


        while (keyInfo.Key != ConsoleKey.Escape && keyInfo.Key != ConsoleKey.Q)
        {
            WriteHeadSection(true);

            if (keyInfo.Key == ConsoleKey.R) { IISRefresh(applicationPool); }

            QueryProcesses(applicationPool);

            WriteFootSection();

            if (keyInfo.Key == ConsoleKey.R) { WriteIISRefresh(); }

            if (keyInfo.Key == ConsoleKey.A)
            {
                WriteAutoMode();
                Thread.Sleep(autoRateMS);
                //TODO: FlushKeyBuffer();
                if (Console.KeyAvailable) { keyInfo = Console.ReadKey(true); }
            }
            else
            {
                FlushKeyBuffer();
                keyInfo = Console.ReadKey(true);
            }
            //TODO: FlushKeyBuffer();
        }
    }

    private static void IISRefresh(List<(string Name, string State)> applicationPool)
    {
        applicationPool.Clear();
        QueryIIS(applicationPool);
    }

    private static void WriteHeadSection(bool tableHeading)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("IISOnline v1.0 2024\n");
        if (tableHeading) { Console.WriteLine("+  IIS NAME                    +  READY    +  LOADED   +  PID/S \n"); }
    }
    private static void WriteFootSection()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n\n   ** Any key to rescan, 'ESC' to kill, 'R' to refresh IIS, 'A' for Auto **\n");
        Console.ResetColor();
    }
    private static void WriteIISRefresh()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n   ** IIS Host's Refreshed **\n");
        Console.ResetColor();
    }
    private static void WriteAutoMode()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n                    ** ( Auto ) **\n");
        Console.ResetColor();
    }
    private static void FlushKeyBuffer()
    {
        while (Console.KeyAvailable) { Console.ReadKey(true); }
    }
    private static void QueryIIS(List<(string Name, string State)> applicationPool)
    {
        try
        {
            using (ServerManager serverManager = new ServerManager())
            {
                foreach (Microsoft.Web.Administration.Site site in serverManager.Sites)
                {
                    applicationPool.Add((site.Name, site.State.ToString()));
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // Not Admin
            Terminate("Insufficient Privileges,\n\n" +
                "Try Running as administrator", ex.Message);
        }
        catch (COMException ex)
        {
            // IIS not installed
            Terminate("COM (Component Object Model) Error occured,\n\n" + 
                "IIS (Internet Information Services) is likely Not Found or Corrupt."
                , ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // IIS installed, W3SVC not found (Started or Stopped)
            Terminate("Invalid Operation Error occured,\n\n" +
                "W3SVC (WWWeb Publishing Service) is likely Not Found or Corrupt."
                , ex.Message);
        }
        catch (Exception ex)
        {
            Terminate("An error occurred communicating with IIS,", ex.Message);
        }
    }



    private static void QueryProcesses(List<(string Name, string State)> applicationPool)
    {
        try
        {
            StringBuilder pidBuilder = new StringBuilder();

            ManagementScope? scope = new ManagementScope(@"\\.\root\cimv2");
            scope.Connect();
            ObjectQuery? query = new ObjectQuery($"SELECT CommandLine, ProcessId FROM Win32_Process WHERE Name = 'w3wp.exe'");

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                var results = searcher.Get().Cast<ManagementObject>().ToList();

                foreach ((string Name, string State) pool in applicationPool)
                {
                    if (pool.State != "Stopped")
                    {
                        foreach (ManagementObject obj in results)
                        {
                            string? commandLine = (string?)obj["CommandLine"];
                            if (commandLine != null && commandLine.Contains($"-ap \"{pool.Name}\" "))
                            {
                                if (pidBuilder.Length > 0) { pidBuilder.Append(", "); }
                                pidBuilder.Append(obj["ProcessId"].ToString());
                            }
                        }
                        if (pidBuilder.Length > 0)
                        {
                            //online in IIS, online in Processes 
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write($"+  {pool.Name.PadRight(26, '_')}  +  {pool.State}  +  Online   +  {pidBuilder}\n");
                            Console.ResetColor();
                            pidBuilder.Clear();
                        }
                        else
                        {
                            //online in IIS, offline in Processes
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write($"+  {pool.Name.PadRight(26, '_')}  +  {pool.State}  +  Offline\n");
                        }
                    }
                    else
                    {
                        //offline in IIS
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write($"+  {pool.Name.PadRight(26, '_')}  +  {pool.State}\n");
                    }
                }
            }


            ObjectQuery memQuery = new ObjectQuery("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
            using (ManagementObjectSearcher memSearcher = new ManagementObjectSearcher(scope, memQuery))
            {
                foreach (ManagementObject obj in memSearcher.Get().Cast<ManagementObject>())
                {
                    ulong freeMemory = (ulong)obj["FreePhysicalMemory"];
                    ulong totalMemory = (ulong)obj["TotalVisibleMemorySize"];
                    ulong usedMemory = totalMemory - freeMemory;
                    double usedMemoryPercentage = (double)usedMemory / totalMemory * 100;

                    if (usedMemoryPercentage < 90)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                    }
                    else 
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }                    
                    Console.WriteLine($"\n+  RAM: {totalMemory / 1024} MB: {usedMemory / 1024} MB: {usedMemoryPercentage:F2}%  +");
                    Console.ResetColor();

                }
            }
        }
        catch (Exception ex)
        {
            Terminate("An error occurred communicating with WMI\n\n" +
                "(Windows Management Interface) / System Processes,", ex.Message);
        }  
        GC.Collect();
    }
    private static void Terminate(string message, string ex)
    {
        WriteHeadSection(false);
        Console.WriteLine(message + "\n");
        Console.WriteLine(ex + "\n");
        Console.WriteLine("Terminal Error, any key to close.");
        Console.ReadKey(true);
        Environment.Exit(1);
    }
}



//Console.ForegroundColor = ConsoleColor.White;
//Console.ForegroundColor = ConsoleColor.Yellow;
//Console.ForegroundColor = ConsoleColor.Magenta;
//Console.ForegroundColor = ConsoleColor.Red;
//Console.ForegroundColor = ConsoleColor.DarkRed;
//Console.ForegroundColor = ConsoleColor.Green;
//Console.ForegroundColor = ConsoleColor.DarkYellow;
//Console.ForegroundColor = ConsoleColor.Cyan;
//Console.ForegroundColor = ConsoleColor.Blue;
//Console.ForegroundColor = ConsoleColor.DarkBlue;
//Console.ForegroundColor = ConsoleColor.DarkCyan;
//Console.ForegroundColor = ConsoleColor.DarkGreen;
//Console.ForegroundColor = ConsoleColor.DarkMagenta;


//ObjectQuery cpuQuery = new ObjectQuery("SELECT Name, PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name != '_Total'");
//using (ManagementObjectSearcher cpuSearcher = new ManagementObjectSearcher(scope, cpuQuery))
//{
//    foreach (ManagementObject obj in cpuSearcher.Get())
//    {
//        double cpuUsage = Convert.ToDouble(obj["PercentProcessorTime"]);
//        string? cpuName = Convert.ToString(obj["Name"]);
//        Console.WriteLine($"+  CPU {cpuName}: {cpuUsage}%");

//    }
//    Console.ResetColor();
//}

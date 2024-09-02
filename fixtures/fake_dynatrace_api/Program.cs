using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.Json;

class Program
{
    static void Main()
    {
        var vcapApplication = Environment.GetEnvironmentVariable("VCAP_APPLICATION");
        if (vcapApplication == null)
        {
            Console.WriteLine("VCAP_APPLICATION environment variable not set.");
            return;
        }

        var application = JsonSerializer.Deserialize<Application>(vcapApplication);
        if (application == null)
        {
            Console.WriteLine("Failed to parse VCAP_APPLICATION.");
            return;
        }

        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");
        listener.Start();
        Console.WriteLine($"Listening on port {port}...");

        while (true)
        {
            var context = listener.GetContext();
            var response = context.Response;

            if (context.Request.Url.AbsolutePath == "/v1/deployment/installer/agent/windows/paas/latest")
            {
                try
                {
                    using (var zipStream = new MemoryStream())
                    {
                        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                        {
                            archive.CreateEntry("agent/lib64/oneagentloader.dll");
                            archive.CreateEntry("agent/conf/ruxitagentproc.conf");
                        }
                        zipStream.Seek(0, SeekOrigin.Begin);
                        zipStream.CopyTo(response.OutputStream);
                    }
                }
                catch (Exception ex)
                {
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.Write(ex.Message);
                    }
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            response.Close();
        }
    }

    public class Application
    {
        public string[] ApplicationURIs { get; set; }
    }
}

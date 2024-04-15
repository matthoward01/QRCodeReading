using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using PdfToImage;
using System.Threading;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace QRCodeReading
{
    class Program
    {
        public static int resolutionInc = 350;
        public static List<string> writeOutLIst = new List<string>();
        static void Main(string[] args)
        {
            bool isThreaded = false;
            Console.WriteLine("Threaded?...y or n Default is n");
            string isThreadedString = Console.ReadLine();
            if (isThreadedString.ToLower().Trim().Equals("y"))
            {
                isThreaded = true;
            }
            Console.WriteLine("Pdf Directory?...");
            string pdfDirectory = Console.ReadLine().Replace("\"", "");
            //Console.WriteLine("Jpg Resolution?...");
            //string resolution = Console.ReadLine().Replace("\"", "");
            if (isThreaded)
            {
                Threaded(pdfDirectory, resolutionInc.ToString());
            }
            else
            {
                UnThreaded(pdfDirectory, resolutionInc.ToString());
            }
            //WriteOut(Path.Combine(pdfDirectory, "QR Code.txt"));
            Console.WriteLine("Done...");
            Console.ReadLine();
        }

        private static void UnThreaded(string pdfDirectory, string resolution)
        {
            string[] files = Directory.GetFiles(pdfDirectory);
            List<string> filesList = files.ToList();
            filesList.Sort();
            if (Directory.Exists("Temp"))
            {
                Directory.Delete("Temp", true);
            }
            Directory.CreateDirectory("Temp");
            string outputFile = Path.Combine(Path.GetFullPath(pdfDirectory), string.Format("{0}", "QRCodes.txt"));
            foreach (string f in filesList)
            {
                if (!f.Contains(".DS_Store") && f.EndsWith(".pdf") && (f.EndsWith(".pdf") || f.EndsWith(".jpg") || f.EndsWith(".jpeg") || f.EndsWith(".tif") || f.EndsWith(".png")))
                {
                    GetImages(f, "Temp\\", outputFile, resolution);
                }
            }
        }

        private static void Threaded(string pdfDirectory, string resolution)
        {
            Console.WriteLine("Thread Count?...");
            string threadString = Console.ReadLine().Replace("\"", "");
            string[] files = Directory.GetFiles(pdfDirectory);
            List<string> filesList = files.ToList();
            filesList.Sort();
            if (Directory.Exists("Temp"))
            {
                Directory.Delete("Temp", true);
            }
            Directory.CreateDirectory("Temp");
            string outputFile = Path.Combine(Path.GetFullPath(pdfDirectory), string.Format("{0}", "QRCodes.txt"));
            int.TryParse(threadString, out int threadLimit);
            List<Thread> finalThreads = new List<Thread>();
            foreach (string f in filesList)
            {
                if (!f.Contains(".DS_Store") && (f.EndsWith(".pdf") || f.EndsWith(".jpg") || f.EndsWith(".jpeg") || f.EndsWith(".tif") || f.EndsWith(".png")))
                {
                    finalThreads.AddRange(GetImagesThreaded(f, "Temp\\", outputFile, resolution));
                }
            }
            foreach (Thread t in finalThreads)
            {
                //Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
                while (Process.GetCurrentProcess().Threads.Count > threadLimit)
                {
                    Thread.Sleep(1000);
                }
                t.Start();
            }
        }

        private static void JpgCreate(string input, string output, int quality, int xRes, int yRes, int firstPage, int lastPage)
        {
            PdfToImage.PDFConvert pp = new PDFConvert();
            pp.OutputFormat = "jpeggray"; //format
            pp.JPEGQuality = quality; //100% quality
            pp.ResolutionX = xRes; //dpi
            pp.ResolutionY = yRes;
            pp.FirstPageToConvert = firstPage; //pages you want
            pp.LastPageToConvert = lastPage;
            pp.Convert(input, output);
        }

        private static void GetImages(string sourcePdf, string tempFolder, string outputFile, string resolution)
        {
            if (sourcePdf.ToLower().EndsWith(".pdf"))
            {
                PdfDocument doc = new PdfDocument();
                doc.LoadFromFile(sourcePdf);
                //List<Thread> listOfThreads = new List<Thread>();
                for (int i = 0; i < doc.Pages.Count; i++)
                {
                    PdfPageBase page = doc.Pages[i];

                    //System.Drawing.Image image = doc.SaveAsImage(0);

                    if (!int.TryParse(resolution, out int res))
                    {
                        Console.WriteLine("Try again using a number for the resolution.");
                    }

                    tempFolder = Path.Combine(tempFolder, string.Format(@"{0}.jpg", Path.GetFileNameWithoutExtension(sourcePdf)));
                    JpgCreate(sourcePdf, tempFolder, 100, res, res, 1, 1);

                    GetQRCodeString(Path.GetFileNameWithoutExtension(sourcePdf), tempFolder, outputFile);
                    //Thread t = new Thread(() => GetQRCodeString(Path.GetFileNameWithoutExtension(sourcePdf), tempFolder, outputFile));
                    //listOfThreads.Add(t);
                }
            }
            else
            {
                if (!int.TryParse(resolution, out int res))
                {
                    Console.WriteLine("Try again using a number for the resolution.");
                }

                JpgCreate(sourcePdf, tempFolder, 100, res, res, 1, 1);

                GetQRCodeString(Path.GetFileNameWithoutExtension(sourcePdf), tempFolder, outputFile);

            }

            //return listOfThreads;
        }
        private static List<Thread> GetImagesThreaded(string sourcePdf, string tempFolder, string outputFile, string resolution)
        {
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(sourcePdf);
            List<Thread> listOfThreads = new List<Thread>();
            for (int i = 0; i < doc.Pages.Count; i++)
            {
                PdfPageBase page = doc.Pages[i];

                //System.Drawing.Image image = doc.SaveAsImage(0);

                if (!int.TryParse(resolution, out int res))
                {
                    Console.WriteLine("Try again using a number for the resolution.");
                }

                tempFolder = Path.Combine(tempFolder, string.Format(@"{0}.jpg", Path.GetFileNameWithoutExtension(sourcePdf)));
                JpgCreate(sourcePdf, tempFolder, 100, res, res, 1, 1);
                
                Thread t = new Thread(() => GetQRCodeString(Path.GetFileNameWithoutExtension(sourcePdf), tempFolder, outputFile));
                listOfThreads.Add(t);
            }

            return listOfThreads;
        }
        private static void ThreadsGo(List<Thread> threads, int threadLimit)
        {
            ThreadPool.SetMinThreads(1, 0);
            ThreadPool.SetMaxThreads(threadLimit, 0);

            foreach (Thread t in threads)
            {
                t.Start();
            }
            Console.ReadLine();
        }
        
        public static string Crop(Bitmap b, Rectangle r, string tempFolder)
        {
            string cropFile = tempFolder;
            using (var nb = new Bitmap(r.Width, r.Height))
            {
                using (Graphics g = Graphics.FromImage(nb))
                {
                    g.DrawImage(b, -r.X, -r.Y);
                    cropFile = Path.Combine(Path.GetDirectoryName(tempFolder), Path.GetFileNameWithoutExtension(tempFolder) + " Crop.jpg");
                    nb.Save(cropFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                
            }
            return cropFile;
        }

        private static void GetQRCodeString(string fileName, string tempFolder, string outputFile)
        {
            try
            {
                //img.Save(tempFolder, System.Drawing.Imaging.ImageFormat.Jpeg);

                //Rectangle rectangle = new Rectangle(620, 400, 750, 300);
                //tempFolder = Crop((Bitmap)Image.FromFile(tempFolder), rectangle, tempFolder);
                IBarcodeReader reader = new BarcodeReader();
                var barcodeBitmap = (Bitmap)Image.FromFile(tempFolder);
                var result = reader.DecodeMultiple(barcodeBitmap);

                if (result != null)
                {
                    resolutionInc = 350;
                    foreach (var r in result)
                    {
                        string resultText = r.Text;
                        Console.WriteLine(fileName + "|" + resultText);
                        using (StreamWriter writer = new StreamWriter(outputFile, append: true))
                        {
                            writer.WriteLine(fileName + "|" + resultText + "|" + Path.GetFileNameWithoutExtension(resultText));
                        }
                        //CheckUrl(resultText);
                        //writeOutLIst.Add(fileName + "|" + resultText + "|" + Path.GetFileNameWithoutExtension(resultText));

                    }
                }
                else
                {
                    resolutionInc += 50;
                    Console.WriteLine(fileName + "|" + "Error Reading...Trying " + resolutionInc.ToString());
                    string path = Path.GetDirectoryName(outputFile);
                    Directory.CreateDirectory(Path.Combine("Temp", resolutionInc.ToString()));
                    GetImages(Path.Combine(Path.GetDirectoryName(outputFile), fileName + ".pdf"), Path.Combine("Temp", resolutionInc.ToString()), outputFile, resolutionInc.ToString());
                    //using (StreamWriter writer = new StreamWriter(outputFile, append: true))
                    //{
                    //    writer.WriteLine(fileName + "|" + "Error Reading...");
                    //}
                    //writeOutLIst.Add(fileName + "|" + "Error Reading...");

                }
            }
            catch (Exception e)
            {
                using (StreamWriter writer = new StreamWriter(outputFile, append: true))
                {
                    writer.WriteLine(fileName + "|" + "Error + " + e.Message);
                }
            }
        }

        private static void CheckUrl(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    /*WebClient wc = new WebClient();
                    string html = wc.DownloadString(url);

                    Regex x = new Regex("<title>(.*)</title>");
                    MatchCollection m = x.Matches(html);
                    if (m.Count > 0)
                    {
                        string title = m[0].Value.Replace("<title>", "").Replace("</title>", "");
                    }
                    client.BaseAddress = new Uri(url);*/
                    //client.DefaultRequestHeaders.Accept.Clear();
                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = client.GetAsync("https://www.carpetone.com/qr/vinyl/M779945").Result;


                    if (response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        var jsonResponse = response.Content.ReadAsStringAsync();
                        Console.WriteLine(jsonResponse);
                        client.Dispose();
                    }
                    else
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void WriteOut(string outputFile)
        {
            using (StreamWriter writer = new StreamWriter(outputFile, append: true))
            {
                foreach (string s in writeOutLIst)
                {
                    writer.WriteLine(s);
                }
            }
            writeOutLIst = new List<string>();
        }
    }
}

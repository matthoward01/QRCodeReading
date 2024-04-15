using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using ZXing;
using PdfToImage;

namespace QRCodeReading
{
    class Program
    {
        public static int resolutionInc = 350;
        public static List<string> writeOutLIst = new List<string>();
        static void Main(string[] args)
        {
            Console.WriteLine("Pdf Directory?...");
            string pdfDirectory = Console.ReadLine().Replace("\"", "");

            GetQrCodes(pdfDirectory, resolutionInc.ToString());

            Console.WriteLine("Done...");
            Console.ReadLine();
        }

        private static void GetQrCodes(string pdfDirectory, string resolution)
        {
            string[] files = Directory.EnumerateFiles(pdfDirectory, "*.*", SearchOption.AllDirectories).ToArray();
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
                if (!f.Contains(".DS_Store") && f.ToLower().EndsWith(".pdf") && (f.ToLower().EndsWith(".pdf") || f.ToLower().EndsWith(".jpg") || f.ToLower().EndsWith(".jpeg") || f.ToLower().EndsWith(".tif") || f.ToLower().EndsWith(".png")))
                {
                    GetImages(f, "Temp\\", outputFile, resolution);
                }
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
                for (int i = 0; i < doc.Pages.Count; i++)
                {
                    if (!int.TryParse(resolution, out int res))
                    {
                        Console.WriteLine("Try again using a number for the resolution.");
                    }

                    tempFolder = Path.Combine(tempFolder, string.Format(@"{0}.jpg", Path.GetFileNameWithoutExtension(sourcePdf)));
                    JpgCreate(sourcePdf, tempFolder, 100, res, res, 1, 1);

                    GetQRCodeString(Path.GetFileNameWithoutExtension(sourcePdf), tempFolder, outputFile);
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
        }

        private static void GetQRCodeString(string fileName, string tempFolder, string outputFile)
        {
            try
            {
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
                    }
                }
                else
                {
                    resolutionInc += 50;
                    Console.WriteLine(fileName + "|" + "Error Reading...Trying " + resolutionInc.ToString());
                    string path = Path.GetDirectoryName(outputFile);
                    Directory.CreateDirectory(Path.Combine("Temp", resolutionInc.ToString()));
                    GetImages(Path.Combine(Path.GetDirectoryName(outputFile), fileName + ".pdf"), Path.Combine("Temp", resolutionInc.ToString()), outputFile, resolutionInc.ToString());
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
    }
}

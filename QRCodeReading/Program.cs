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

namespace QRCodeReading
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Pdf Directory?...");
            string pdfDirectory = Console.ReadLine().Replace("\"", "");
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
                if (!f.Contains(".DS_Store") && f.EndsWith(".pdf"))
                {
                    GetImages(f, "Temp\\", outputFile);
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

        private static void GetImages(string sourcePdf, string tempFolder, string outputFile)
        {
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(sourcePdf);

            for (int i = 0; i < doc.Pages.Count; i++)
            {
                PdfPageBase page = doc.Pages[i];

                //System.Drawing.Image image = doc.SaveAsImage(0);
                

                tempFolder = Path.Combine(tempFolder, string.Format(@"{0}.jpg", Path.GetFileNameWithoutExtension(sourcePdf)));
                JpgCreate(sourcePdf, tempFolder, 100, 1200, 1200, 1, 1);

                GetQRCodeString(Path.GetFileNameWithoutExtension(sourcePdf), tempFolder, outputFile);
 
            }
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
            //img.Save(tempFolder, System.Drawing.Imaging.ImageFormat.Jpeg);

            //Rectangle rectangle = new Rectangle(620, 400, 750, 300);
            //tempFolder = Crop((Bitmap)Image.FromFile(tempFolder), rectangle, tempFolder);
            IBarcodeReader reader = new BarcodeReader();
            var barcodeBitmap = (Bitmap)Image.FromFile(tempFolder);   
            var result = reader.DecodeMultiple(barcodeBitmap);

            if (result != null)
            {
                foreach (var r in result)
                {
                    string resultText = r.Text;
                    Console.WriteLine(fileName + "|" + resultText);
                    using (StreamWriter writer = new StreamWriter(outputFile, append: true))
                    {
                        writer.WriteLine(fileName + "|" + resultText);
                    }
                }                
            }
            else
            {
                Console.WriteLine(fileName + "|" + "Error Reading...");
                using (StreamWriter writer = new StreamWriter(outputFile, append: true))
                {
                    writer.WriteLine(fileName + "|" + "Error Reading...");
                }
            }
        }
    }
}

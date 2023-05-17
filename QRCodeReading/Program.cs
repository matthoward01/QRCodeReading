using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

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
            string outputFile = Path.Combine(Path.GetFileNameWithoutExtension(pdfDirectory), string.Format("{0}", "QRCodes.txt"));
            foreach (string f in filesList)
            {
                if (!f.Contains(".DS_Store") && f.EndsWith(".pdf"))
                {
                    GetImages(f, "Temp\\", outputFile);
                }
            }
        }

        private static void GetImages(string sourcePdf, string tempFolder, string outputFile)
        {
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(sourcePdf);

            for (int i = 0; i < doc.Pages.Count; i++)
            {
                PdfPageBase page = doc.Pages[i];

                System.Drawing.Image image = doc.SaveAsImage(0);

                tempFolder = Path.Combine(tempFolder, string.Format(@"{0}.jpg", Path.GetFileNameWithoutExtension(sourcePdf)));

                GetQRCodeString(Path.GetFileNameWithoutExtension(sourcePdf), image, tempFolder, outputFile);
 
            }
        }

        private static void GetQRCodeString(string fileName, System.Drawing.Image img, string tempFolder, string outputFile)
        {
            img.Save(tempFolder, System.Drawing.Imaging.ImageFormat.Jpeg);

            IBarcodeReader reader = new BarcodeReader();
            var barcodeBitmap = (Bitmap)Image.FromFile(tempFolder);   
            var result = reader.Decode(barcodeBitmap);

            if (result != null)
            {
                string resultText = result.Text;
                Console.WriteLine(resultText);
                using (StreamWriter writer = new StreamWriter(outputFile, append: true))
                {
                    writer.WriteLine(fileName + "|" + resultText);
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

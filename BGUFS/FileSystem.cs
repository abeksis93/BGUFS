using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace BGUFS
{
    class FileSystem
    {
        Dictionary<string, FileMetaData> dict;
        string path;

        public FileSystem()
        {
        }

        // Convert an object to a byte array
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }

        public bool create(string fileSystemPath)
        {
            dict = new Dictionary<string, FileMetaData>();
            try
            {
                // Create the file, or overwrite if the file exists.
                using (FileStream fs = File.Create(fileSystemPath))
                {
                    byte[] info = ObjectToByteArray(dict);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        public bool add(string filesystem, string filename)
        {
            readFileSystem(filesystem);
            FileInfo fi = new FileInfo(filename);
            if (this.dict.ContainsKey(fi.Name))
            {
                Console.WriteLine("file already exist");
                return false;
            }
            FileMetaData fmd = new FileMetaData(fi.Name, fi.Length, fi.CreationTime, "regular", EncodeFile(filename));
            dict.Add(fi.Name, fmd);
            update(filesystem);
            return true;
        }

        public bool remove(string filesystem, string filename)
        {
            readFileSystem(filesystem);
            if (!this.dict.ContainsKey(filename))
            {
                Console.WriteLine("file does not exist");
                return false;
            }
            this.dict.Remove(filename);
            update(filesystem);
            return true;
        }

        public bool rename(string filesystem, string filename, string newfilename)
        {
            readFileSystem(filesystem);
            if (!this.dict.ContainsKey(filename))
            {
                Console.WriteLine("file does not exist");
                return false;
            }
            if (this.dict.ContainsKey(newfilename))
            {
                Console.WriteLine("file {0} already exists", newfilename);
                return false;
            }
            FileMetaData file = this.dict[filename];
            file.setFileName(newfilename);
            this.dict.Remove(filename);
            dict.Add(newfilename, file);
            update(filesystem);
            return true;
        }

        public bool extract(string filesystem, string filename, string target)
        {
            readFileSystem(filesystem);
            if (!this.dict.ContainsKey(filename))
            {
                Console.WriteLine("file does not exist");
                return false;
            }
            DecodeFile(this.dict[filename].getFileData(), target);
            return true;
        }

        public bool dir(string filesystem)
        {
            readFileSystem(filesystem);
            foreach (FileMetaData fmd in dict.Values)
            {
                fmd.printFileMetaData();
            }
            return true;
        }

        private bool update(string fileSystemPath)
        {
            try
            {
                // Create the file, or overwrite if the file exists.
                using (FileStream fs = File.Create(fileSystemPath))
                {
                    byte[] info = ObjectToByteArray(this.dict);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        private bool readFileSystem(string fileSystemPath)
        {
            try
            {
                // Open the stream and read it back.
                byte[] arr = File.ReadAllBytes(fileSystemPath);
                Object dictObj = ByteArrayToObject(arr);
                this.dict = (Dictionary<string, FileMetaData>)dictObj;
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        private	string generateMD5Hash(string filename)
        {
            return "";
        }

        private void DecodeFile(string srcfile, string destfile)
        {
            byte[] bt64 = System.Convert.FromBase64String(srcfile);
            if (File.Exists(destfile))
            {
                File.Delete(destfile);
            }
            FileStream sw = new FileStream(destfile, FileMode.Create);
            sw.Write(bt64, 0, bt64.Length);
            sw.Close();
        }
        private string EncodeFile(string srcfile)
        {
            string dest;
            FileStream sr = new FileStream(srcfile, FileMode.Open);
            byte[] srcbt = new byte[sr.Length];
            sr.Read(srcbt, 0, (int)sr.Length);
            sr.Close();
            return System.Convert.ToBase64String(srcbt);
        }

        static void Main(string[] args)
        {
            string filePath = "MYBGUFS.dat";
            string filename1 = @"C:\Users\yoni9\Desktop\testfldr\src\txttest.txt";
            string filename2 = @"C:\Users\yoni9\Desktop\testfldr\src\pngtest.png"; 
            string filename3 = @"C:\Users\yoni9\Desktop\testfldr\src\docxtest.docx";
            string filename4 = @"C:\Users\yoni9\Desktop\testfldr\src\pdftest.pdf";
            string filename5 = @"C:\Users\yoni9\Desktop\testfldr\src\pttxtest.pptx";
            string filename6 = @"C:\Users\yoni9\Desktop\testfldr\src\xslxtest.xlsx";
            string target1 = @"C:\Users\yoni9\Desktop\testfldr\target\txttest.txt";
            string target2 = @"C:\Users\yoni9\Desktop\testfldr\target\pngtest.png";
            string target3 = @"C:\Users\yoni9\Desktop\testfldr\target\docxtest.docx";
            string target4 = @"C:\Users\yoni9\Desktop\testfldr\target\pdftest.pdf";
            string target5 = @"C:\Users\yoni9\Desktop\testfldr\target\pttxtest.pptx";
            string target6 = @"C:\Users\yoni9\Desktop\testfldr\target\xslxtest.xlsx";
            string filenameclean1 = "txttest.txt";
            string filenameclean2 = "pngtest.png";
            string filenameclean3 = "docxtest.docx";
            string filenameclean4 = "pdftest.pdf";
            string filenameclean5 = "pttxtest.pptx";
            string filenameclean6 = "xslxtest.xlsx";
            string fileRenameTest = "testRename1.txt";
            string fileExractAfterRenameTest = @"C:\Users\yoni9\Desktop\testfldr\target\testExractAfterRename.txt";
            FileSystem fs = new FileSystem();
            fs.create(filePath);
            fs.add(filePath, filename1);
            fs.add(filePath, filename2);
            fs.dir(filePath);
            fs.extract(filePath, filename3, target3);
            Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            Console.WriteLine("--------------------------------");
            fs.add(filePath, filename1);
            Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            fs.add(filePath, filename3);
            fs.add(filePath, filename4);
            fs.add(filePath, filename5);
            fs.add(filePath, filename6);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.extract(filePath, filenameclean1, target1);
            fs.extract(filePath, filenameclean2, target2);
            fs.extract(filePath, filenameclean3, target3);
            fs.extract(filePath, filenameclean4, target4);
            fs.extract(filePath, filenameclean5, target5);
            fs.extract(filePath, filenameclean6, target6);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.remove(filePath, filenameclean4);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.remove(filePath, filenameclean4);
            Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            Console.WriteLine("--------------------------------");
            fs.rename(filePath, filenameclean1, fileRenameTest);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.rename(filePath, filenameclean1, fileRenameTest);
            Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            Console.WriteLine("--------------------------------");
            fs.rename(filePath, filenameclean2, fileRenameTest);
            Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            Console.WriteLine("--------------------------------");
            fs.extract(filePath, fileRenameTest, fileExractAfterRenameTest);

        }
    }
}

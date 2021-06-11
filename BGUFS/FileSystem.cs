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
            if (this.dict.ContainsKey(filename))
            {
                Console.WriteLine("file already exist");
                return false;
            }

            FileInfo fi = new FileInfo(filename);
            FileMetaData fmd = new FileMetaData(fi.Name, fi.Length, fi.CreationTime, System.IO.File.ReadAllBytes(filename));
            dict.Add(filename, fmd);
            create(filesystem);
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

        // add get file

        //private FileMetaData getMetaData(string filename)
        //{
        //    return new FileMetaData();

        //}

        private	string generateMD5Hash(string filename)
        {
            return "";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string filePath = "MYBGUFS.dat";
            string filename = @"C:\Users\yoni9\source\repos\BGUFS\test.txt";
            FileSystem fs = new FileSystem();
            fs.create(filePath);
            fs.add(filePath, filename);
        }
    }
}

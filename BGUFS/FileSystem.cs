using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace BGUFS
{
    class FileSystem
    {
        Dictionary<string, FileMetaData> dict;
        Dictionary<string, string> files;
        //List<long> holeIndexes;
        //long startWriterIndex;

        public FileSystem()
        {
        }

        public bool create(string fileSystemPath)
        {
            string fileSystemChecker = "BGUFS_";
            dict = new Dictionary<string, FileMetaData>();
            //holeIndexes = new List<long>();
            files = new Dictionary<string, string>();
            try
            {
                // Create the file in append mode
                using (FileStream fs = new FileStream(fileSystemPath, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    byte[] fscInBytes = ObjectToByteArray(fileSystemChecker);
                    byte[] dictInfo = ObjectToByteArray(dict);
                    //byte[] holeBytes = ObjectToByteArray(holeIndexes);
                    byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
                    byte[] filesBytes = ObjectToByteArray(files);
                    int fscLength = fscInBytes.Length;
                    int dictInfoLength = dictInfo.Length;
                    //int holeBytesLength = holeBytes.Length;
                    int filesBytesLength = filesBytes.Length;
                    int bytesCounter = 0;
                    byte[] lineBytes = Encoding.ASCII.GetBytes(fscLength.ToString());
                    bytesCounter += lineBytes.Length;
                    //lineBytes = Encoding.ASCII.GetBytes(holeBytesLength.ToString());
                    //bytesCounter += lineBytes.Length;
                    lineBytes = Encoding.ASCII.GetBytes(dictInfoLength.ToString());
                    bytesCounter += lineBytes.Length;
                    lineBytes = Encoding.ASCII.GetBytes(filesBytesLength.ToString());
                    bytesCounter += lineBytes.Length;

                    //long startWriterIndex = fscLength + dictInfoLength + filesBytesLength + (3 * newLine.Length);
                    //lineBytes = Encoding.ASCII.GetBytes(startWriterIndex.ToString());
                    //bytesCounter += lineBytes.Length;
                    //startWriterIndex += bytesCounter;

                    //sw.WriteLine(startWriterIndex);
                    sw.WriteLine(fscLength.ToString());
                    sw.WriteLine(dictInfoLength.ToString());
                    //sw.WriteLine(holeBytesLength.ToString());
                    sw.WriteLine(filesBytesLength.ToString());
                    sw.Flush();
                    fs.Write(fscInBytes);
                    fs.Write(newLine);
                    fs.Write(dictInfo);
                    fs.Write(newLine);
                    //fs.Write(holeBytes);
                    //fs.Write(newLine);
                    fs.Write(filesBytes);
                    fs.Write(newLine);
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
            bool exists = readHeader(filesystem);
            if (!exists)
                return false;
            FileInfo fileInfo = new FileInfo(filename);
            if (this.dict.ContainsKey(fileInfo.Name))
            {
                Console.WriteLine("file already exist");
                return false;
            }
            //long fileStartIndex = 0;
            //if (this.dict.Count == 0)
            //    fileStartIndex = this.startWriterIndex;
            //else
            //    fileStartIndex = readNextAvailable(filesystem);

            //this.startWriterIndex += fileInfo.Length;
            FileMetaData fmd = new FileMetaData(fileInfo.Name, fileInfo.Length, fileInfo.CreationTime, "regular");
            dict.Add(fileInfo.Name, fmd);
            using (FileStream fs = new FileStream(filesystem, FileMode.Append, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                string fileData = EncodeFile(filename);
                sw.WriteLine(fileData);
                files.Add(fileInfo.Name, fileData);
            }
            update(filesystem);
            return true;
        }

        //private int readNextAvailable(string filesystem)
        //{
        //    using (StreamReader sr = new StreamReader(filesystem))
        //    {
        //        string line = sr.ReadLine();
        //        int nextAvailableIndex = Int32.Parse(line);
        //        return nextAvailableIndex;
        //    }
        //}

        public bool remove(string filesystem, string filename)   //TODO add remove links
        {
            FileInfo fileInfo = new FileInfo(filename);
            bool exists = readHeader(filesystem);
            if (!exists)
                return false;
            if (!this.dict.ContainsKey(fileInfo.Name))
            {
                Console.WriteLine("file does not exist");
                return false;
            }
            FileMetaData fmd;
            this.dict.TryGetValue(fileInfo.Name, out fmd);
            this.dict.Remove(fileInfo.Name);
            //add file to hole
            //holeIndexes.Add(fmd.getStartIndex());
            update(filesystem);
            return true;
        }

        public bool rename(string filesystem, string filename, string newfilename)
        {
            bool exists = readHeader(filesystem);
            if (!exists)
                return false;
            FileInfo fileInfo = new FileInfo(filename);
            FileInfo newFileInfo = new FileInfo(newfilename);
            if (!this.dict.ContainsKey(fileInfo.Name))
            {
                Console.WriteLine("file does not exist");
                return false;
            }
            if (this.dict.ContainsKey(newFileInfo.Name))
            {
                Console.WriteLine("file {0} already exists", newfilename);
                return false;
            }
            FileMetaData fmd = this.dict[fileInfo.Name];
            fmd.setFileName(newfilename);
            this.dict.Remove(fileInfo.Name);
            this.files.Remove(fileInfo.Name);
            //add file to hole
            //holeIndexes.Add(fmd.getStartIndex());
            files.Add(newFileInfo.Name, EncodeFile(filename));
            dict.Add(newFileInfo.Name, fmd);
            update(filesystem);
            return true;
        }

        public bool extract(string filesystem, string filename, string extractedfilename)
        {
            bool exists = readHeader(filesystem);
            if (!exists)
                return false;
            FileInfo fileInfo = new FileInfo(filename);
            if (!this.dict.ContainsKey(fileInfo.Name))
            {
                Console.WriteLine("file does not exist");
                return false;
            }
            
            DecodeFile(this.files[filename], extractedfilename);
            return true;
        }

        public bool dir(string filesystem)
        {
            readHeader(filesystem);
            foreach (FileMetaData fmd in dict.Values)
            {
                fmd.printFileMetaData();
            }
            return true;
        }

        public string hash(string fileSystem, string filename)
        {
            bool exists = readHeader(fileSystem);
            if (!exists)
                return null;
            FileInfo fileInfo = new FileInfo(filename);
            if (!this.dict.ContainsKey(fileInfo.Name))
            {
                Console.WriteLine("file does not exist");
                return null;
            }
            return generateMD5Hash(filename);
        }

        private bool readHeader(string fileSystemPath)
        {
            int bytesCounter = 0;
            int fscLength = 0;
            int dictInfoLength = 0;
            //int holeBytesLength = 0;
            int filesBytesLength = 0;
            int startReadIndex = 0;
            byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
            using (StreamReader sr = new StreamReader(fileSystemPath))
            {
                string line = sr.ReadLine();
                byte[] lineBytes = Encoding.ASCII.GetBytes(line);
                bytesCounter += lineBytes.Length;
                //this.startWriterIndex = Int32.Parse(line);
                fscLength = Int32.Parse(line);
                bytesCounter += (3 * newLine.Length);
                line = sr.ReadLine();
                byte[] line2Bytes = Encoding.ASCII.GetBytes(line);
                bytesCounter += line2Bytes.Length;
                dictInfoLength = Int32.Parse(line);
                line = sr.ReadLine();
                byte[] line3Bytes = Encoding.ASCII.GetBytes(line);
                bytesCounter += line3Bytes.Length;
                filesBytesLength = Int32.Parse(line);
                //line = sr.ReadLine();
                //byte[] line4Bytes = Encoding.ASCII.GetBytes(line);
                //bytesCounter += line4Bytes.Length;
                //holeBytesLength = Int32.Parse(line);
                //line = sr.ReadLine();
                //byte[] line5Bytes = Encoding.ASCII.GetBytes(line);
                //bytesCounter += line5Bytes.Length;
                sr.Close();
            }

            byte[] fscInBytes = new byte[fscLength];
            byte[] dictInfo = new byte[dictInfoLength];
            //byte[] holeBytes = new byte[holeBytesLength];
            byte[] filesBytes = new byte[filesBytesLength];
            startReadIndex = bytesCounter;
            using (BinaryReader reader = new BinaryReader(new FileStream(fileSystemPath, FileMode.Open)))
            {
                reader.BaseStream.Seek(startReadIndex, SeekOrigin.Begin);
                reader.Read(fscInBytes, 0, fscLength);
                string str = Encoding.UTF8.GetString(fscInBytes);
                if (!str.Contains("BGUFS_"))
                {
                    Console.WriteLine("Not a BGUFS file");
                    return false;
                }
                startReadIndex = bytesCounter + fscInBytes.Length + newLine.Length;
                reader.BaseStream.Seek(startReadIndex, SeekOrigin.Begin);
                reader.Read(dictInfo, 0, dictInfoLength);
                Object dictObj = ByteArrayToObject(dictInfo);
                this.dict = (Dictionary<string, FileMetaData>)dictObj;
                startReadIndex += dictInfoLength + newLine.Length;
                reader.BaseStream.Seek(startReadIndex, SeekOrigin.Begin);
                //reader.Read(holeBytes, 0, holeBytesLength);
                //Object listObj = ByteArrayToObject(holeBytes);
                //this.holeIndexes = (List<long>)listObj;
                //startReadIndex += holeBytesLength + newLine.Length;
                //reader.BaseStream.Seek(startReadIndex, SeekOrigin.Begin);
                reader.Read(filesBytes, 0, filesBytesLength);
                Object filesObj = ByteArrayToObject(filesBytes);
                this.files = (Dictionary<string, string>)filesObj;
            }
            return true;
        }


        private bool update(string fileSystemPath)
        {
            string tmpFilePath = "tmp.dat";
            string fileSystemChecker = "BGUFS_";
            try
            {
                // Create the file in append mode
                using (FileStream fs = new FileStream(tmpFilePath, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    byte[] fscInBytes = ObjectToByteArray(fileSystemChecker);
                    byte[] dictInfo = ObjectToByteArray(dict);
                    //byte[] holeBytes = ObjectToByteArray(holeIndexes);
                    byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
                    byte[] filesBytes = ObjectToByteArray(files);
                    int fscLength = fscInBytes.Length;
                    int dictInfoLength = dictInfo.Length;
                    //int holeBytesLength = holeBytes.Length;
                    int filesBytesLength = filesBytes.Length;

                    //sw.WriteLine(startWriteIndex.ToString()); 
                    sw.WriteLine(fscLength.ToString());
                    sw.WriteLine(dictInfoLength.ToString());
                    //sw.WriteLine(holeBytesLength.ToString());
                    sw.WriteLine(filesBytesLength.ToString());
                    sw.Flush();
                    fs.Write(fscInBytes);
                    fs.Write(newLine);
                    fs.Write(dictInfo);
                    fs.Write(newLine);
                    //fs.Write(holeBytes);
                    //fs.Write(newLine);
                    fs.Write(filesBytes);
                    foreach (string key in files.Keys)
                    {
                        string val = this.files[key];
                        sw.WriteLine(val);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            File.Delete(fileSystemPath);
            File.Move(tmpFilePath, fileSystemPath);

            return true;
        }


        private string generateMD5Hash(string filename)
        {
            using (var md5Instance = MD5.Create())
            { 
                FileInfo fileInfo = new FileInfo(filename);
                string fileData = this.files[fileInfo.Name];
                byte[] fileBytes = Encoding.ASCII.GetBytes(fileData);
                byte[] hashBytes = md5Instance.ComputeHash(fileBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public void optimize(string filesystem)
        {
            //TODO
        }

        public bool sortAB(string filesystem)
        {
            bool exists = readHeader(filesystem);
            if (!exists)
                return false;
            SortedDictionary<string, FileMetaData> sortedDict = new SortedDictionary<string, FileMetaData>(this.dict);
            this.dict = sortedDict.ToDictionary(pair => pair.Key, pair => pair.Value);
            update(filesystem);
            return true;
        }

        //private bool updateAfterSort(string filesystem, SortedDictionary<string, FileMetaData> sortedDict, long startWriteIndex)
        //{
        //    string tmpFilePath = "tmp.dat";
        //    string fileSystemChecker = "BGUFS_";
        //    try
        //    {
        //        // Create the file in append mode
        //        using (FileStream fs = new FileStream(tmpFilePath, FileMode.Append, FileAccess.Write))
        //        using (StreamWriter sw = new StreamWriter(fs))
        //        {
        //            byte[] fscInBytes = ObjectToByteArray(fileSystemChecker);
        //            byte[] dictInfo = ObjectToByteArray(sortedDict);
        //            byte[] holeBytes = ObjectToByteArray(holeIndexes);
        //            byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
        //            byte[] filesBytes = ObjectToByteArray(files);
        //            int fscLength = fscInBytes.Length;
        //            int dictInfoLength = dictInfo.Length;
        //            int holeBytesLength = holeBytes.Length;
        //            int filesBytesLength = filesBytes.Length;

        //            sw.WriteLine(startWriteIndex.ToString());
        //            sw.WriteLine(fscLength.ToString());
        //            sw.WriteLine(dictInfoLength.ToString());
        //            sw.WriteLine(holeBytesLength.ToString());
        //            sw.WriteLine(filesBytesLength.ToString());
        //            sw.Flush();
        //            fs.Write(fscInBytes);
        //            fs.Write(newLine);
        //            fs.Write(dictInfo);
        //            fs.Write(newLine);
        //            fs.Write(holeBytes);
        //            fs.Write(newLine);
        //            fs.Write(filesBytes);
        //            foreach (string key in files.Keys)
        //            {
        //                string val = this.files[key];
        //                sw.WriteLine(val);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        return false;
        //    }
        //    File.Delete(filesystem);
        //    File.Move(tmpFilePath, filesystem);

        //    return true;
        //}

        private void DecodeFile(string srcfile, string destfile)
        {
            byte[] bt64 = Convert.FromBase64String(srcfile);
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
        static void Main(string[] args)
        {
            string filePath = "MYBGUFS.dat";
            string filename3 = @"C:\Users\user\Downloads\docxtest.docx";
            string filename4 = @"C:\Users\user\Downloads\pdftest.pdf";
            string filename5 = @"C:\Users\user\Downloads\pttxtest.pptx";
            string target1 = @"C:\Users\user\Downloads\txttest.txt";
            string target2 = @"C:\Users\\user\Downloads\pngtest.png";
            string filenameclean1 = "txttest.txt";
            string filenameclean2 = "QueenOfHearts.png";
            string fileRenameTest = "testRename1.txt";
            string fileExractAfterRenameTest = @"C:\Users\user\Downloads\testExractAfterRename.txt";
            FileSystem fs = new FileSystem();
            fs.create(filePath);
            fs.add(filePath, filenameclean1);
            fs.add(filePath, filenameclean2);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.add(filePath, filename3);
            fs.add(filePath, filename4);
            fs.add(filePath, filename5);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.extract(filePath, filenameclean1, target1);
            fs.extract(filePath, filenameclean2, target2);
            fs.remove(filePath, filename3);
            fs.dir(filePath);
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
            string hash = fs.hash(filePath, filename4);
            Console.WriteLine(hash);
            string hash2 = fs.hash(filePath, filenameclean2);
            Console.WriteLine(hash2);
            Console.WriteLine("--------------------------------");
            fs.sortAB(filePath);
            fs.dir(filePath);
            

            //////main//////
            //FileSystem fileSystem = new FileSystem();

            //switch(args[0])
            //{
            //    case "-create":
            //        fileSystem.create(args[1]);
            //        break;

            //    case "-add":
            //        fileSystem.add(args[1], args[2]);
            //        break;

            //    case "-remove":
            //        fileSystem.remove(args[1], args[2]);
            //        break;

            //    case "-rename":
            //        fileSystem.rename(args[1], args[2], args[3]);
            //        break;

            //    case "-extract":
            //        fileSystem.extract(args[1], args[2], args[3]);
            //        break;

            //    case "-dir":
            //        fileSystem.dir(args[1]);
            //        break;

            //    case "-hash":
            //        fileSystem.hash(args[1], args[2]);
            //        break;

            //    case "-optimize":
            //        fileSystem.optimize(args[1]);
            //        break;

            //    case "-sortAB":
            //        fileSystem.sortAB(args[1]);
            //        break;

            //    case "-sortDate":
            //        fileSystem.sortDate(args[1]);
            //        break;

            //    case "-sortSize":
            //        fileSystem.sortSize(args[1]);
            //        break;

            //    case "-addLink":
            //        fileSystem.addLink(args[1], args[2], args[3]);
            //        break;

            //}

        }
    }
}

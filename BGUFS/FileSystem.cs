using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace BGUFS
{
    class FileSystem
    {
        Dictionary<string, FileMetaData> dict;
        //List<byte[]> files;
        List<long> holeIndexes;
        long startWriterIndex;

        public FileSystem()
        {
        }

        public bool create(string fileSystemPath)
        {
            string fileSystemChecker = "BGUFS_";
            dict = new Dictionary<string, FileMetaData>();
            holeIndexes = new List<long>();
            try
            {
                // Create the file in append mode
                using (FileStream fs = new FileStream(fileSystemPath, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    byte[] fscInBytes = ObjectToByteArray(fileSystemChecker);
                    byte[] dictInfo = ObjectToByteArray(dict);
                    byte[] holeBytes = ObjectToByteArray(holeIndexes);
                    byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
                    int fscLength = fscInBytes.Length;
                    int dictInfoLength = dictInfo.Length;
                    int holeBytesLength = holeBytes.Length;
                    int bytesCounter = 0;
                    byte[] lineBytes = Encoding.ASCII.GetBytes(fscLength.ToString());
                    bytesCounter += lineBytes.Length;
                    lineBytes = Encoding.ASCII.GetBytes(holeBytesLength.ToString());
                    bytesCounter += lineBytes.Length;
                    lineBytes = Encoding.ASCII.GetBytes(dictInfoLength.ToString());
                    bytesCounter += lineBytes.Length;

                    long startWriterIndex = fscLength + dictInfoLength + holeBytesLength + (3 * newLine.Length);
                    lineBytes = Encoding.ASCII.GetBytes(startWriterIndex.ToString());
                    bytesCounter += lineBytes.Length;
                    startWriterIndex += bytesCounter;

                    sw.WriteLine(startWriterIndex);
                    sw.WriteLine(fscLength.ToString());
                    sw.WriteLine(dictInfoLength.ToString());
                    sw.WriteLine(holeBytesLength.ToString());
                    sw.Flush();
                    fs.Write(fscInBytes);
                    fs.Write(newLine);
                    fs.Write(dictInfo);
                    fs.Write(newLine);
                    fs.Write(holeBytes);
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
            long fileStartIndex = 0;
            if (this.dict.Count == 0)
                fileStartIndex = this.startWriterIndex;
            else
                fileStartIndex = readNextAvailable(filesystem);

            this.startWriterIndex += fileInfo.Length;
            FileMetaData fmd = new FileMetaData(fileInfo.Name, fileInfo.Length, fileInfo.CreationTime, "regular", fileStartIndex);
            dict.Add(fileInfo.Name, fmd);
            update(filesystem, this.startWriterIndex);
            return true;
        }

        private int readNextAvailable(string filesystem)
        {
            using (StreamReader sr = new StreamReader(filesystem))
            {
                string line = sr.ReadLine();
                int nextAvailableIndex = Int32.Parse(line);
                return nextAvailableIndex;
            }
        }

        public bool remove(string filesystem, string filename)
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
            holeIndexes.Add(fmd.getStartIndex());
            update(filesystem, startWriterIndex);
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
            //add file to hole
            holeIndexes.Add(fmd.getStartIndex());
            dict.Add(newFileInfo.Name, fmd);
            update(filesystem, startWriterIndex);
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
            
            DecodeFile(EncodeFile(filename), extractedfilename);
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

        private bool readHeader(string fileSystemPath)
        {
            int bytesCounter = 0;
            int fscLength = 0;
            int dictInfoLength = 0;
            int holeBytesLength = 0;
            int startReadIndex = 0;
            byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
            using (StreamReader sr = new StreamReader(fileSystemPath))
            {
                string line = sr.ReadLine();
                byte[] lineBytes = Encoding.ASCII.GetBytes(line);
                bytesCounter += lineBytes.Length;
                this.startWriterIndex = Int32.Parse(line);
                bytesCounter += (4 * newLine.Length);
                line = sr.ReadLine();
                byte[] line2Bytes = Encoding.ASCII.GetBytes(line);
                bytesCounter += line2Bytes.Length;
                fscLength = Int32.Parse(line);
                line = sr.ReadLine();
                byte[] line3Bytes = Encoding.ASCII.GetBytes(line);
                bytesCounter += line3Bytes.Length;
                dictInfoLength = Int32.Parse(line);
                line = sr.ReadLine();
                byte[] line4Bytes = Encoding.ASCII.GetBytes(line);
                bytesCounter += line4Bytes.Length;
                holeBytesLength = Int32.Parse(line);
                sr.Close();
            }

            byte[] fscInBytes = new byte[fscLength];
            byte[] dictInfo = new byte[dictInfoLength];
            byte[] holeBytes = new byte[holeBytesLength];
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
                reader.Read(holeBytes, 0, holeBytesLength);
                Object listObj = ByteArrayToObject(holeBytes);
                this.holeIndexes = (List<long>)listObj;
                startReadIndex += holeBytesLength + newLine.Length;
            }
            return true;
        }


        private bool update(string fileSystemPath, long startWriteIndex)
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
                    byte[] holeBytes = ObjectToByteArray(holeIndexes);
                    byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
                    int fscLength = fscInBytes.Length;
                    int dictInfoLength = dictInfo.Length;
                    int holeBytesLength = holeBytes.Length;

                    sw.WriteLine(startWriteIndex.ToString()); 
                    sw.WriteLine(fscLength.ToString());
                    sw.WriteLine(dictInfoLength.ToString());
                    sw.WriteLine(holeBytesLength.ToString());
                    sw.Flush();
                    fs.Write(fscInBytes);
                    fs.Write(newLine);
                    fs.Write(dictInfo);
                    fs.Write(newLine);
                    fs.Write(holeBytes);
                    fs.Write(newLine);
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
                using (var stream = File.OpenRead(filename))
                {
                    var hashResult = md5Instance.ComputeHash(stream);
                    return BitConverter.ToString(hashResult);
                }
            }
        }

        private int listContains(List<FileMetaData> fmdLst, string filename)
        {
            for (int i = 0; i < fmdLst.Count; i++)
            {
                if (fmdLst[i].getFileName().Equals(filename))
                {
                    return i;
                }
            }
            return -1;
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

        //private void ReplaceFile(string FilePath, string NewFilePath)
        //{
        //    using (StreamReader vReader = new StreamReader(FilePath))
        //    {
        //        using (StreamWriter vWriter = new StreamWriter(NewFilePath))
        //        {
        //            int vLineNumber = 0;
        //            while (!vReader.EndOfStream)
        //            {
        //                string vLine = vReader.ReadLine();
        //                if (vLineNumber == 0)
        //                {
        //                    continue;
        //                }
        //                else if(vLineNumber == 1)
        //                {
        //                    vWriter.WriteLine(ObjectToByteArray(this.dict));
        //                }
        //                else if (vLineNumber == 2)
        //                {
        //                    vWriter.WriteLine(ObjectToByteArray(this.holeIndexes));
        //                }
        //                vLineNumber++;
        //            }
        //        }
        //    }
        //}


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
            //string filename1 = @"C:\Users\yoni9\Desktop\testfldr\src\txttest.txt";
            //string filename2 = @"C:\Users\yoni9\Desktop\testfldr\src\pngtest.png"; 
            string filename3 = @"C:\Users\user\Downloads\docxtest.docx";
            string filename4 = @"C:\Users\user\Downloads\pdftest.pdf";
            string filename5 = @"C:\Users\user\Downloads\pttxtest.pptx";
            //string filename6 = @"C:\Users\yoni9\Desktop\testfldr\src\xslxtest.xlsx";
            string target1 = @"C:\Users\user\Downloads\txttest.txt";
            string target2 = @"C:\Users\\user\Downloads\pngtest.png";
            //string target3 = @"C:\Users\yoni9\Desktop\testfldr\target\docxtest.docx";
            //string target4 = @"C:\Users\yoni9\Desktop\testfldr\target\pdftest.pdf";
            //string target5 = @"C:\Users\yoni9\Desktop\testfldr\target\pttxtest.pptx";
            //string target6 = @"C:\Users\yoni9\Desktop\testfldr\target\xslxtest.xlsx";
            string filenameclean1 = "txttest.txt";
            string filenameclean2 = "QueenOfHearts.png";
            //string filenameclean3 = "docxtest.docx";
            //string filenameclean4 = "pdftest.pdf";
            //string filenameclean5 = "pttxtest.pptx";
            //string filenameclean6 = "xslxtest.xlsx";
            string fileRenameTest = "testRename1.txt";
            //string fileExractAfterRenameTest = @"C:\Users\yoni9\Desktop\testfldr\target\testExractAfterRename.txt";
            FileSystem fs = new FileSystem();
            fs.create(filePath);
            fs.add(filePath, filenameclean1);
            fs.add(filePath, filenameclean2);
            fs.dir(filePath);
            //fs.extract(filePath, filename3, target3);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            Console.WriteLine("--------------------------------");
            //fs.add(filePath, filename1);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            fs.add(filePath, filename3);
            fs.add(filePath, filename4);
            fs.add(filePath, filename5);
            //fs.add(filePath, filename6);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.extract(filePath, filenameclean1, target1);
            fs.extract(filePath, filenameclean2, target2);
            //fs.extract(filePath, filenameclean3, target3);
            //fs.extract(filePath, filenameclean4, target4);
            //fs.extract(filePath, filenameclean5, target5);
            //fs.extract(filePath, filenameclean6, target6);
            //fs.dir(filePath);
            //Console.WriteLine("--------------------------------");
            fs.remove(filePath, filename3);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            //fs.remove(filePath, filenameclean4);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            //Console.WriteLine("--------------------------------");
            fs.rename(filePath, filenameclean1, fileRenameTest);
            fs.dir(filePath);
            Console.WriteLine("--------------------------------");
            fs.rename(filePath, filenameclean1, fileRenameTest);
            Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            Console.WriteLine("--------------------------------");
            fs.rename(filePath, filenameclean2, fileRenameTest);
            Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            Console.WriteLine("--------------------------------");
            //fs.extract(filePath, fileRenameTest, fileExractAfterRenameTest);


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

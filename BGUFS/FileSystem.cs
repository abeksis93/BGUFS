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
        string path;
        List<int> holeIndexes;

        public FileSystem()
        {
        }

        public bool create(string fileSystemPath)
        {
            string fileSystemChecker = "BGUFS_";
            dict = new Dictionary<string, FileMetaData>();
            holeIndexes = new List<int>();
            try
            {
                // Create the file, or overwrite if the file exists.
                using (FileStream fs = new FileStream(fileSystemPath, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    byte[] fscInBytes = ObjectToByteArray(fileSystemChecker);
                    byte[] dictInfo = ObjectToByteArray(dict);
                    byte[] holeBytes = ObjectToByteArray(holeIndexes);
                    //int fscLength = fscInBytes.Length;
                    //int dictInfoLength = dictInfo.Length;
                    byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);

                    // Add some information to the file.
                    //sw.WriteLine(fscLength.ToString());
                    //sw.WriteLine(dictInfoLength.ToString());
                    //sw.Flush();
                    fs.Write(fscInBytes);
                    fs.Write(newLine);
                    fs.Write(dictInfo);
                    fs.Write(newLine);
                    fs.Write(holeBytes);
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
            readHeader(filesystem);
            FileInfo fi = new FileInfo(filename);
            if (this.dict.ContainsKey(fi.Name))
            {
                Console.WriteLine("file already exist");
                return false;
            }

            int fileStartIndex = this.dict.Count + 3;
            FileMetaData fmd = new FileMetaData(fi.Name, fi.Length, fi.CreationTime, "regular", fileStartIndex);
            dict.Add(fi.Name, fmd);
            Console.WriteLine("before update");
            update(filesystem);
            Console.WriteLine("after update");
            return true;
        }

        public bool remove(string filesystem, string filename)
        {
            readHeader(filesystem);
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
            readHeader(filesystem);
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
            readHeader(filesystem);
            if (!this.dict.ContainsKey(filename))
            {
                Console.WriteLine("file does not exist");
                return false;
            }
            //DecodeFile(this.dict[filename].getFileData(), target);
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

        //private bool update(string fileSystemPath)
        //{
        //    string fileSystemChecker = "BGUFS_";
        //    try
        //    {
        //        // Create the file, or overwrite if the file exists.
        //        using (FileStream fs = new FileStream(fileSystemPath, FileMode.Append, FileAccess.Write))
        //        using (StreamWriter sw = new StreamWriter(fs))
        //        {
        //            byte[] fscInBytes = ObjectToByteArray(fileSystemChecker);
        //            byte[] dictInfo = ObjectToByteArray(this.dict);
        //            byte[] holeBytes = ObjectToByteArray(this.holeIndexes);
        //            //int fscLength = fscInBytes.Length;
        //            //int dictInfoLength = dictInfo.Length;
        //            byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);

        //            // Add some information to the file.
        //            //sw.WriteLine(fscLength.ToString());
        //            //sw.WriteLine(dictInfoLength.ToString());
        //            //sw.Flush();
        //            fs.Write(fscInBytes);
        //            fs.Write(newLine);
        //            fs.Write(dictInfo);
        //            fs.Write(newLine);
        //            fs.Write(holeBytes);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        return false;
        //    }
        //    return true;
        //}

        private byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private bool readHeader(string fileSystemPath)
        {
            try
            {
                // Open the stream and read it back.
                byte[] arr = File.ReadAllBytes(fileSystemPath);
                byte[] temp;
                int counter = 0;
                int pastI = -1;
                //char[] arrStr = System.Text.Encoding.UTF8.GetString(arr).ToCharArray();
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == 0xA)
                    {
                        if (counter == 0)
                        {
                            pastI = i;
                            temp = SubArray(arr, 0, pastI);
                            string str = Encoding.UTF8.GetString(temp);
                            Console.WriteLine(str);
                            if (! str.Contains("BGUFS_"))
                            {
                                Console.WriteLine("Not a BGUFS file");
                                return false;
                            }
                        }
                        else if (counter == 1)
                        {
                            temp = SubArray(arr, pastI + 1, i);
                            pastI = i;
                            Object dictObj = ByteArrayToObject(temp);
                            this.dict = (Dictionary<string, FileMetaData>)dictObj;
                        }
                        else if (counter == 2)
                        {
                            temp = SubArray(arr, pastI + 1, i);
                            Object listObj = ByteArrayToObject(temp);
                            this.holeIndexes = (List<int>)listObj;
                        }
                        else
                        {
                            break;
                        }
                        counter++;
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }



        private bool update(string fileSystemPath)
        {
            try
            {
                //create new file = "tmp.dat"
                //copy old file to new
                //insted of copy old dict write new  dict in byte[]
                // " " " holesIndexs " " " " "
                //delete old file
                //rename "tmp.dat" => "MYBGUFS.dat"
                byte[] arr = File.ReadAllBytes(fileSystemPath);
                string tmpFilePath = "tmp.dat";
                char[] arrStr = System.Text.Encoding.UTF8.GetString(arr).ToCharArray();
                using (FileStream fs = new FileStream(tmpFilePath, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    byte[] temp;
                    int counter = 0;
                    int pastI = -1;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr[i] == 0xA)
                        {
                            if (counter == 0)
                            {
                                pastI = i;
                                temp = SubArray(arr, 0, pastI);
                                string str = Encoding.UTF8.GetString(temp);
                                if (!str.Contains("BGUFS_"))
                                {
                                    Console.WriteLine("Not a BGUFS file");
                                    return false;
                                }
                                fs.Write(temp);
                            }
                            else if (counter == 1)
                            {
                                temp = SubArray(arr, pastI + 1, i);
                                pastI = i;
                                fs.Write(ObjectToByteArray(this.dict));
                            }
                            else if (counter == 2)
                            {
                                temp = SubArray(arr, pastI + 1, i);
                                pastI = i;
                                fs.Write(ObjectToByteArray(this.holeIndexes));
                            }
                            else
                            {
                                temp = SubArray(arr, pastI + 1, i);
                                pastI = i;
                                fs.Write(temp);
                            }
                            counter++;
                        }
                    }
                }
                File.Delete(fileSystemPath);
                File.Move(tmpFilePath, fileSystemPath);
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
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
            string filename1 = @"C:\Users\yoni9\Desktop\testfldr\src\txttest.txt";
            //string filename2 = @"C:\Users\yoni9\Desktop\testfldr\src\pngtest.png"; 
            //string filename3 = @"C:\Users\yoni9\Desktop\testfldr\src\docxtest.docx";
            //string filename4 = @"C:\Users\yoni9\Desktop\testfldr\src\pdftest.pdf";
            //string filename5 = @"C:\Users\yoni9\Desktop\testfldr\src\pttxtest.pptx";
            //string filename6 = @"C:\Users\yoni9\Desktop\testfldr\src\xslxtest.xlsx";
            //string target1 = @"C:\Users\yoni9\Desktop\testfldr\target\txttest.txt";
            //string target2 = @"C:\Users\yoni9\Desktop\testfldr\target\pngtest.png";
            //string target3 = @"C:\Users\yoni9\Desktop\testfldr\target\docxtest.docx";
            //string target4 = @"C:\Users\yoni9\Desktop\testfldr\target\pdftest.pdf";
            //string target5 = @"C:\Users\yoni9\Desktop\testfldr\target\pttxtest.pptx";
            //string target6 = @"C:\Users\yoni9\Desktop\testfldr\target\xslxtest.xlsx";
            string filenameclean1 = "txttest.txt";
            string filenameclean2 = "pngtest.png";
            string filenameclean3 = "docxtest.docx";
            string filenameclean4 = "pdftest.pdf";
            string filenameclean5 = "pttxtest.pptx";
            string filenameclean6 = "xslxtest.xlsx";
            string fileRenameTest = "testRename1.txt";
            //string fileExractAfterRenameTest = @"C:\Users\yoni9\Desktop\testfldr\target\testExractAfterRename.txt";
            FileSystem fs = new FileSystem();
            fs.create(filePath);
            fs.add(filePath, filename1);
            //fs.add(filePath, filename2);
            //fs.dir(filePath);
            //fs.extract(filePath, filename3, target3);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            //Console.WriteLine("--------------------------------");
            //fs.add(filePath, filename1);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            //fs.add(filePath, filename3);
            //fs.add(filePath, filename4);
            //fs.add(filePath, filename5);
            //fs.add(filePath, filename6);
            //fs.dir(filePath);
            //Console.WriteLine("--------------------------------");
            //fs.extract(filePath, filenameclean1, target1);
            //fs.extract(filePath, filenameclean2, target2);
            //fs.extract(filePath, filenameclean3, target3);
            //fs.extract(filePath, filenameclean4, target4);
            //fs.extract(filePath, filenameclean5, target5);
            //fs.extract(filePath, filenameclean6, target6);
            //fs.dir(filePath);
            //Console.WriteLine("--------------------------------");
            //fs.remove(filePath, filenameclean4);
            //fs.dir(filePath);
            //Console.WriteLine("--------------------------------");
            //fs.remove(filePath, filenameclean4);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            //Console.WriteLine("--------------------------------");
            //fs.rename(filePath, filenameclean1, fileRenameTest);
            //fs.dir(filePath);
            //Console.WriteLine("--------------------------------");
            //fs.rename(filePath, filenameclean1, fileRenameTest);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            //Console.WriteLine("--------------------------------");
            //fs.rename(filePath, filenameclean2, fileRenameTest);
            //Console.WriteLine(" ^^^^ Shuold print Error ^^^^ ");
            //Console.WriteLine("--------------------------------");
            //fs.extract(filePath, fileRenameTest, fileExractAfterRenameTest);

        }
    }
}

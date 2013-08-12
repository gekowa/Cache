using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using CacheAspect.Supporting;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CacheAspect
{
    public class DiskCache : ICache
    {
        object LOCK_READ = new object();
        object LOCK_WRITE = new object();

        public DiskCache() { }

        public object this[string key]
        {
            get
            {
                FileStream fs = null;
                string filePath = GetFilePathByKey(key);
                try
                {
                    lock (LOCK_READ)
                    {
                        if (!File.Exists(filePath))
                        {
                            return null;
                        }
                        fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        IFormatter f = new BinaryFormatter();
                        MethodExecWrapper result = f.Deserialize(fs) as MethodExecWrapper;
                        fs.Close();
                        return result;
                    }
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    return null;
                }
                catch (IOException)
                {
                    return null;
                }
                //catch (SerializationException)
                //{
                //    File.Delete(filePath);
                //    return null;
                //}
                finally
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                }
            }
            set
            {
                FileStream fs = null;
                string filePath = GetFilePathByKey(key);
                try
                {
                    lock (LOCK_WRITE)
                    {
                        string path = filePath.Substring(0, filePath.LastIndexOf('\\'));
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        IFormatter f = new BinaryFormatter();
                        f.Serialize(fs, value);
                        fs.Flush();
                        fs.Close();
                    }
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                // May already exists
                catch (IOException) { }
                finally
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                }
            }
        }

        public bool Contains(string key)
        {
            string filePath = GetFilePathByKey(key);
            return File.Exists(filePath);
        }

        public void Delete(string key)
        {
            string filePath = GetFilePathByKey(key);
            try
            {
                File.Delete(filePath);
            }
            catch (FileNotFoundException) { }
            catch (IOException) { }
        }

        public void DeleteSimilar(string key)
        {
            string filePath = GetFilePathByKey(key);
            try
            {
                if (File.Exists(filePath))
                {
                    // is a file
                    File.Delete(filePath);
                }
                else if (Directory.Exists(filePath))
                {
                    // is a directory
                    Directory.Delete(filePath, true);
                }
            }
            catch (DirectoryNotFoundException) { }
            catch (FileNotFoundException) { }
            catch (IOException) { }
        }


        static string GetFilePathByKey(string key)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                key = key.Replace(c, '_');
            }

            return CacheService.DiskPath +
                key.Replace('\x20', '_').Replace(KeyBuilder.SPLITTER, '\\');
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace GSIWin_Script_Tool
{
    static class Util
    {
        public static bool PathIsFolder(string path)
        {
            return new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory);
        }
    }
    public static class InstructionName
    {

        static readonly Dictionary<byte, string> instname = new Dictionary<byte, string>
        {
            { 0x00, "RET   " },
            { 0x01, "THROW1" },
            { 0x02, "LOAD0" },
            { 0x03, "LOAD1" },
            { 0x04, "LOAD2" },
            { 0x05, "LOAD3" },
            { 0x06, "LOAD4" },
            { 0x07, "LOAD5" },
            { 0x08, "LOAD6" },
            { 0x09, "LOAD7" },
            { 0x0A, "PUSH  COMPRESSED MES" },
            { 0x0B, "PUSH             MES" },
            { 0x0C, "STORE0" },
            { 0x0D, "STORE1" },
            { 0x0E, "STORE2" },
            { 0x0F, "STORE3" },
            { 0x10, "STORE4" },
            { 0x11, "STORE5" },
            { 0x12, "STORE6" },
            { 0x13, "STORE7" },
            { 0x14, "JX0   DWORD" },
            { 0x15, "JMP   DWORD" },
            //{ 0x16, "unk   " },
            //{ 0x17, "unk   " },
            //{ 0x18, "unk   " },
            { 0x19, "MESJ  " },
            { 0x1A, "JX2   DWORD" },
            { 0x1B, "SELJ  " },
            { 0x1C, "ESCAPE" },
            { 0x32, "PUSH  DWORD" },
            { 0x33, "PUSH             STR" },
            { 0x34, "ADD   " },
            { 0x35, "SUB   " },
            { 0x36, "MUL   " },
            { 0x37, "DIV   " },
            { 0x38, "MOD   " },
            { 0x39, "RAND  " },
            { 0x3A, "LAND  " },
            { 0x3B, "LOR   " },
            { 0x3C, "AND   " },
            { 0x3D, "OR    " },
            { 0x3E, "LT    " },
            { 0x3F, "GT    " },
            { 0x40, "LE    " },
            { 0x41, "GE    " },
            { 0x42, "EQ    " },
            { 0x43, "NE    " },
        };

        public static string GetName(byte ins)
        {
            if (instname.ContainsKey(ins))
                return $"{ins:X2}\t{instname[ins]}";
            else
                return $"{ins:X2}\tUNK";
        }
    }

    public class JsonHelper
    {
        #region 对象类型序列化为json 字符
        /// <summary>
        /// 对象类型序列化为json 字符
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="jsonObject">待转换实体</param>
        /// <param name="encoding">编码格式</param>
        /// <returns>string</returns>
        public static string ObjectToJson<T>(Object jsonObject, Encoding encoding)
        {
            string result = String.Empty;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                serializer.WriteObject(ms, jsonObject);
                result = encoding.GetString(ms.ToArray());
            }
            return result;
        }
        #endregion

        #region json字符反序列化为对象
        /// <summary>
        /// json字符反序列化为对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="json">json字符串</param>
        /// <param name="encoding">编码格式</param>
        /// <returns>T</returns>
        public static T JsonToObject<T>(string json, Encoding encoding)
        {
            T resultObject = default(T);
            try
            {
                resultObject = Activator.CreateInstance<T>();
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(encoding.GetBytes(json)))
                {
                    resultObject = (T)serializer.ReadObject(ms);
                }
            }
            catch { }
            return resultObject;
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GSIWin_Script_Tool
{
    using TString = Tuple<int, string>;

    class Script
    {
        public Script()
        {
        }

        public void Load(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = new BinaryReader(stream))
            {
                Read(reader);
            }
        }

        static readonly Encoding _encoding = Encoding.GetEncoding("shift_jis");

        readonly List<Opcode> Opcodes = new List<Opcode>();

        readonly Scripts scripts = new Scripts();

        void Read(BinaryReader reader)
        {
            scripts.tbl1 = reader.ReadInt32();
            scripts.tbl2 = reader.ReadInt32();

            for (int i = 0; i < scripts.tbl1; i++)
            {
                scripts.TextOpOffset.Add(reader.ReadUInt32());
            }

            for (int i = 0; i < scripts.tbl2; i++)
            {
                scripts.Offset2.Add(reader.ReadUInt32());
            }

            Opcodes.Clear();

            long codebase = reader.BaseStream.Position;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                uint addr = Convert.ToUInt32(reader.BaseStream.Position - codebase);
                byte code = reader.ReadByte();

                switch (code)
                {
                    case 0x00: // THROW
                    case 0x01: // THROW
                    case 0x02: // LOAD
                    case 0x03: // LOAD
                    case 0x04: // LOAD
                    case 0x05: // LOAD
                    case 0x06: // LOAD
                    case 0x07: // LOAD
                    case 0x08: // LOAD
                    case 0x09: // LOAD
                    case 0x0C: // STORE
                    case 0x0D: // STORE
                    case 0x0E: // STORE
                    case 0x0F: // STORE
                    case 0x10: // STORE
                    case 0x11: // STORE
                    case 0x12: // STORE
                    case 0x13: // STORE
                    case 0x17:
                    case 0x18:
                    case 0x34: // ADD
                    case 0x35: // SUB
                    case 0x36: // MUL
                    case 0x37: // DIV
                    case 0x38: // MOD
                    case 0x39: // RANDOM
                    case 0x3A: // LAND
                    case 0x3B: // LOR
                    case 0x3C: // AND
                    case 0x3D: // OR
                    case 0x3E: // LT
                    case 0x3F: // GT
                    case 0x40: // LE
                    case 0x41: // GE
                    case 0x42: // EQ
                    case 0x43: // NE
                    //case 0xFF:
                    //case 0xFE:
                    //case 0xFD:
                    //case 0xFC:
                    //case 0xFB:
                    //case 0xFA:
                    {
                        Opcodes.Add(new Opcode(addr, code));
                        break;
                    }
                    case 0x0A: // STR
                    case 0x0B: // STR
                    case 0x33: // PUSH STR
                    {
                        var args = reader.ReadCString(true);
                        Opcodes.Add(new Opcode(addr, code, args));
                        break;
                    }
                    case 0x14: // JZ DWORD [FIX]
                    case 0x15: // JMP DWORD [FIX]
                    case 0x19:
                    case 0x1A:
                    case 0x1B: // JMP DWORD [FIX]
                    case 0x32: // PUSH DWORD
                    {
                        if (code == 0x19)   //Start of the text block
                        {
                            if (!scripts.TextOpOffset.Contains(addr))
                            {
                                throw new Exception("Address not found in table.");
                            }
                        }

                        var args = reader.ReadBytes(4);
                        Opcodes.Add(new Opcode(addr, code, args));
                        break;
                    }
                    case 0x1C:
                    {
                        var args = reader.ReadBytes(1);
                        Opcodes.Add(new Opcode(addr, code, args));
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Unknown Opcode {code:X2}");
                    }
                }
            }
        }

//Down here is the brand new modifications
        public void Disas(string filename)
        {
            StreamWriter sw = new StreamWriter(filename + ".txt");
            foreach (var op in Opcodes)
            {
                var name = InstructionName.GetName(op.Code);
                if (op.Args == null)
                    sw.WriteLine($"{op.Addr:X8}: {name}");
                else
                {
                    switch (op.Code)
                    {
                        case 0x0A: // STR
                        case 0x0B: // STR
                        case 0x33: // PUSH STR
                        {
                            if (op.Code == 0xA)
                                sw.WriteLine($"{op.Addr:X8}: {name}, \"{ReadCompressedString(op.Args)}\"");
                            else
                                sw.WriteLine($"{op.Addr:X8}: {name}, \"{ReadCString(op.Args)}\"");
                            break;
                        }
                        case 0x14: // JZ DWORD [FIX]
                        case 0x15: // JMP DWORD [FIX]
                        case 0x19:
                        case 0x1A:
                        case 0x1B: // JMP DWORD [FIX]
                        case 0x32: // PUSH DWORD
                        {
                            sw.WriteLine($"{op.Addr:X8}: {name}, 0x{Endian.Reverse(BitConverter.ToUInt32(op.Args, 0)):X8}");
                            break;
                        }
                        case 0x1C: //ESCAPE SEQUENCE
                        {
                            if (op.Args[0] == 0) //0x1C00
                            {
                                    sw.WriteLine($"{op.Addr:X8}: {op.Code:X2}\tNEWLINE");
                            }
                            else if (op.Args[0] == 1)    //0x1C01
                            {
                                sw.WriteLine($"{op.Addr:X8}: {op.Code:X2}\tRUBY");
                            }
                            else
                            {
                                sw.WriteLine($"{op.Addr:X8}: {name}, 0x{op.Args[0]:X2}");
                            }  
                            break;
                        }
                        default:
                            sw.WriteLine($"{op.Addr:X8}: {name}");
                            break;
                    }
                }
            }
            sw.Flush();
            sw.Close();
        }


        [Serializable]
        class CommandSector
        {
            public bool IsText;
            public byte TextType;
            public List<Opcode> Opcodes;

            public CommandSector()
            {
                IsText = false;
                TextType = 0;
                Opcodes = new List<Opcode>();
            }
        }

        [Serializable]
        class CommandBlock
        {
            public bool ContainsText;
            public uint BlockType;
            public int Index;
            public List<CommandSector> Sectors;

            public CommandBlock()
            {
                ContainsText = false;
                BlockType = 0;
                Index = 0;
                Sectors = new List<CommandSector>();
            }
        }

        [Serializable]
        class Scripts
        {
            public int tbl1;
            public int tbl2;
            public List<uint> TextOpOffset;
            public List<uint> Offset2;
            public List<CommandBlock> Commands;

            public Scripts()
            {
                tbl1 = 0;
                tbl2 = 0;
                TextOpOffset = new List<uint>();
                Offset2 = new List<uint>();
                Commands = new List<CommandBlock>();
            }
        }
        CommandSector NewSector(int down, int upper, byte type)
        {
            var cs = new CommandSector();
            for (int j = down / 2; j < upper / 2; ++j)
                cs.Opcodes.Add(Opcodes[j]);
            cs.IsText = type == 0 ? false : true;
            cs.TextType = type;
            return cs;
        }
        public void Anal(string filePath, bool output)
        {
            List<int> textindex = new List<int>();
            textindex.Add(0);

            StringBuilder strB = new StringBuilder();
            foreach (var op in Opcodes)
            {
                strB.Append(op.Code.ToString("X2"));
            }
                

            var opsequence = strB.ToString();
            var r = Regex.Matches(opsequence, @"1[9B]0A(.*?)32|19(.*?)0E");

            for (int i = 0; i < r.Count; ++i)
            {
                textindex.Add(r[i].Index);
            }

            textindex.Add(opsequence.Length);

            int tindex = 1;
            for (int i = 0; i < textindex.Count - 1; ++i)
            {
                var cm = new CommandBlock();

                var ibase = textindex[i];
                var iend = textindex[i + 1];

                var tmp = opsequence.Substring(ibase, iend - ibase);
                var m = Regex.Match(tmp, @"19.*?0E(33).*?(32)32.*?0E(0A).*?(32)32");
                if (m.Success)
                {
                    var p0 = m.Groups[1].Index;
                    var p1 = m.Groups[2].Index;
                    var p2 = m.Groups[3].Index;
                    var p3 = m.Groups[4].Index;

                    cm.Sectors.Add(NewSector(ibase, ibase + p0, 0));
                    cm.Sectors.Add(NewSector(ibase + p0, ibase + p1, 5));
                    cm.Sectors.Add(NewSector(ibase + p1, ibase + p2, 0));
                    cm.Sectors.Add(NewSector(ibase + p2, ibase + p3, 2));
                    cm.Sectors.Add(NewSector(ibase + p3, iend, 0));

                    cm.ContainsText = true;
                    cm.Index = tindex;
                    tindex++;

                }
                else
                {
                    m = Regex.Match(tmp, @"19.*?0E(0A).*?(32)32");
                    if (m.Success)
                    {
                        var p0 = m.Groups[1].Index;
                        var p1 = m.Groups[2].Index;

                        cm.Sectors.Add(NewSector(ibase, ibase + p0, 0));
                        cm.Sectors.Add(NewSector(ibase + p0, ibase + p1, 3));
                        cm.Sectors.Add(NewSector(ibase + p1, iend, 0));

                        cm.ContainsText = true;
                        cm.Index = tindex;
                        tindex++;
                    }
                    else
                    {
                        m = Regex.Match(tmp, @"19(0A).*?(32)32");
                        if (m.Success)
                        {
                            var p0 = m.Groups[1].Index;
                            var p1 = m.Groups[2].Index;

                            cm.Sectors.Add(NewSector(ibase, ibase + p0, 0));
                            cm.Sectors.Add(NewSector(ibase + p0, ibase + p1, 1));
                            cm.Sectors.Add(NewSector(ibase + p1, iend, 0));

                            cm.ContainsText = true;
                            cm.Index = tindex;
                            tindex++;
                        }
                        else
                        {
                            m = Regex.Match(tmp, @"1B(0A).*?(00)32");
                            if (m.Success)
                            {
                                var p0 = m.Groups[1].Index;
                                var p1 = m.Groups[2].Index;

                                cm.Sectors.Add(NewSector(ibase, ibase + p0, 0));
                                cm.Sectors.Add(NewSector(ibase + p0, ibase + p1, 4));
                                cm.Sectors.Add(NewSector(ibase + p1, iend, 0));

                                cm.ContainsText = true;
                                cm.Index = tindex;
                                tindex++;
                            }
                            else
                            {
                                cm.Sectors.Add(NewSector(ibase, iend, 0));
                            }
                        }
                    }
                }
                scripts.Commands.Add(cm);
            }
            if (output)
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    sw.WriteLine(JsonHelper.ObjectToJson<Scripts>(scripts, _encoding));
                    sw.Flush();
                    sw.Close();
                }
            }
        }
        class TransStr
        {
            public string str;
            public string ruby;
            //ruby text?
            public bool type;
            public TransStr(string inp, string rub)
            {
                str = inp;
                ruby = rub;
                type = true;
            }
            public TransStr(string inp)
            {
                str = inp;
                ruby = "";
                type = false;
            }
        }
        class ImportedTransText
        {
            public string character;
            public string text;
            public ImportedTransText(string t)
            {
                text = t;
            }
            public ImportedTransText(string c, string t)
            {
                character = c;
                text = t;
            }
        }
        List<ImportedTransText> BulidTransText(string filePath)
        {
            var ret = new List<ImportedTransText>();
            ret.Add(new ImportedTransText("dummy"));
            var orig = new List<string>();
            using (StreamReader reader = File.OpenText(filePath))
            {
                while (!reader.EndOfStream)
                {
                    orig.Add(reader.ReadLine());
                }
            }

            for(int i = 0; i < orig.Count;)
            {
                if (orig[i][0] == '○')
                {
                    do
                    {
                        ++i;
                    } while (orig[i][0] != '●');

                    var chara = orig[i].Substring(orig[i].IndexOf('●', 1) + 1, orig[i].LastIndexOf('●') - (orig[i].IndexOf('●', 1) + 1));
                    ++i;
                    StringBuilder sb = new StringBuilder();
                    do
                    {
                        if (i > orig.Count - 1 || orig[i][0] == '○')
                        {
                            sb.Remove(sb.Length - 1, 1);
                            break;
                        }
                        sb.Append(orig[i]);
                        sb.Append('\n');
                        ++i;
                    } while (true);

                    if (chara != "")
                    {
                        ret.Add(new ImportedTransText(chara, sb.ToString()));
                    }
                    else
                    {
                        ret.Add(new ImportedTransText(sb.ToString()));
                    }
                }
                else
                {
                    throw new Exception("Text Lineup Failed");
                }
            }
                
            return ret;
        }
        CommandSector NewString(uint pos, string str, byte mode)
        {
            CommandSector cs = new CommandSector();
            var translated_encoding = Encoding.GetEncoding("gbk");
            if (mode == 0)//Compressed text
            {
                var lst = new List<TransStr>();

                var splitstr = str.Split('\n');
                foreach (var sub in splitstr)
                {
                    var r = Regex.Matches(sub, @"(\{.*?\:.*?\})");
                    if (r.Count > 0)
                    {
                        lst.Add(new TransStr(sub.Substring(0, r[0].Groups[1].Index)));
                        for (int i = 0; i < r.Count; ++i)
                        {
                            var ruby = r[i].Groups[1].Value;
                            lst.Add(new TransStr(ruby.Substring(1, ruby.IndexOf(':') - 1),ruby.Substring(ruby.IndexOf(':') + 1, ruby.IndexOf('}') - ruby.IndexOf(':') - 1)));
                            if (i == r.Count - 1)
                            {
                                lst.Add(new TransStr(sub.Substring(r[r.Count - 1].Groups[1].Index + r[r.Count - 1].Groups[1].Length)));
                            }
                            else
                            {
                                lst.Add(new TransStr(sub.Substring(r[i].Groups[1].Index + r[i].Groups[1].Length, r[i + 1].Groups[1].Index - (r[i].Groups[1].Index + r[i].Groups[1].Length))));
                            }
                        }
                    }
                    else
                    {
                        lst.Add(new TransStr(sub));
                    }
                    //add linebreak sign
                    lst.Add(new TransStr("lb"));
                }
                lst.RemoveAt(lst.Count - 1);
                uint lbc = 0;
                for (int i = 0; i < lst.Count; ++i)
                {
                    if (lst[i].str == "")
                        continue;
                    else if (lst[i].str == "lb")
                    {
                        cs.Opcodes.Add(new Opcode(0, 0x1C, new byte[] { 0 }));//NewLine
                        lbc++;
                        if (lbc > 3)
                        {
                            throw new Exception($"LineBreak count is bigger than 3");
                        }
                        continue;
                    }
                        
                    var bytes = translated_encoding.GetBytes(UnespaceString(lst[i].str));
                    // String is null terminated
                    var args = new byte[bytes.Length + 1];
                    Buffer.BlockCopy(bytes, 0, args, 0, bytes.Length);

                    Opcode op = new Opcode(0, 0x0A, args);

                    if (lst[i].type)
                    {
                        var bytes2 = translated_encoding.GetBytes(UnespaceString(lst[i].ruby));
                        var args2 = new byte[bytes2.Length + 1];
                        Buffer.BlockCopy(bytes2, 0, args2, 0, bytes2.Length);

                        cs.Opcodes.Add(new Opcode(0, 0x1C, new byte[] { 1 }));
                        cs.Opcodes.Add(new Opcode(0, 0x0A, args2));
                        cs.Opcodes.Add(new Opcode(0, 0x00));
                        cs.Opcodes.Add(op);
                    }
                    else
                    {
                        cs.Opcodes.Add(op);
                    }
                }
            }
            else
            {
                var bytes = translated_encoding.GetBytes(UnespaceString(str));
                var args = new byte[bytes.Length + 1];
                Buffer.BlockCopy(bytes, 0, args, 0, bytes.Length);

                cs.Opcodes.Add(new Opcode(pos, 0x33, args));
            }
            cs.Opcodes[0].Addr = pos;
            return cs;
        }
        public void Rebulid(string filePath)
        {
            var lst = BulidTransText(filePath);
            Opcodes.Clear();
            int textindex = 1;
            foreach(var it in scripts.Commands)
            {
                if (it.ContainsText)
                {
                    Debug.Assert(it.Index == textindex);
                    for(int i = 0; i < it.Sectors.Count; ++i)
                    {
                        if(it.Sectors[i].IsText)
                        {
                            if (it.Sectors[i].TextType == 5)
                            {
                                it.Sectors[i] = NewString(it.Sectors[i].Opcodes[0].Addr, lst[textindex].character, 1);
                            }   
                            else
                            {
                                it.Sectors[i] = NewString(it.Sectors[i].Opcodes[0].Addr, lst[textindex].text, 0);
                            }
                            foreach (var ssub in it.Sectors[i].Opcodes)
                            {
                                Opcodes.Add(ssub);
                            }
                        }
                        else
                        {
                            foreach (var ssub in it.Sectors[i].Opcodes)
                            {
                                Opcodes.Add(ssub);
                            }
                        }
                    }
                    textindex++;
                }
                else
                {
                    foreach (var sub in it.Sectors)
                    {
                        foreach (var ssub in sub.Opcodes)
                        {
                            Opcodes.Add(ssub);
                        }
                    }            
                }
            }
        }

        public void Save(string filePath)
        {
            // Build code section

            var offset_table_0 = new List<uint>();
            var offset_table_1 = new List<uint>();

            byte[] section_code;

            using (var stream = new MemoryStream(4096))
            using (var writer = new BinaryWriter(stream))
            {
                // Build code

                foreach (var item in Opcodes)
                {
                    uint addr = Convert.ToUInt32(stream.Position);

                    // Update opcode address
                    item.NewAddr = addr;

                    // Write code
                    writer.Write(item.Code);

                    // Write argument
                    if (item.Args != null)
                    {
                        writer.Write(item.Args);
                    }
                }

                // Update address

                var opcodeMap = Opcodes.ToDictionaryEx(a => a.Addr, b => b.NewAddr);

                foreach (var item in Opcodes)
                {
                    switch (item.Code)
                    {
                        case 0x14:
                        case 0x15:
                        case 0x1A:
                        case 0x1B:
                        {
                            // Get jump address
                            var addr = BitConverter.ToUInt32(item.Args, 0);
                            addr = Endian.Reverse(addr);

                            // Find jump target
                            if (!opcodeMap.TryGetValue(addr, out addr))
                                throw new Exception("Jump target not found.");

                            // Update address
                            addr = Endian.Reverse(addr);

                            // Write new target address
                            stream.Position = item.NewAddr + 1;
                            writer.Write(addr);

                            break;
                        }
                        case 0x19:
                        {
                            offset_table_0.Add(item.NewAddr);
                            break;
                        }
                            /*case 0x1A:
                            {
                                offset_table_1.Add(item.NewAddr + 1 + 4);
                                break;
                            }*/
                    }
                }

                writer.Flush();

                section_code = stream.ToArray();
            }

            // Write script file

            using (var stream = File.Create(filePath))
            using (var writer = new BinaryWriter(stream))
            {
                // Write table size
                writer.Write(offset_table_0.Count);
                writer.Write(offset_table_1.Count);

                // Write table 0
                foreach (var item in offset_table_0)
                {
                    writer.Write(item);
                }

                // Write table 1
                foreach (var item in offset_table_1)
                {
                    writer.Write(item);
                }

                // Write code
                writer.Write(section_code);

                // Finished
                writer.Flush();
            }
        }

        [Serializable]
        class Opcode
        {
            public uint Addr;
            public uint NewAddr;
            public byte Code;
            public byte[] Args;

            public Opcode(uint addr, byte code)
            {
                Addr = addr;
                Code = code;
                Args = null;
            }

            public Opcode(uint addr, byte code, byte[] args)
            {
                Addr = addr;
                Code = code;
                Args = args;
            }
        }
        IList<TString> GetStrings()
        {
            var list = new List<TString>();

            for (int i = 0; i < Opcodes.Count; i++)
            {
                var opcode = Opcodes[i];

                switch (opcode.Code)
                {
                    case 0x0A:
                    {
                        var str = ReadCompressedString(opcode.Args);
                        list.Add(new TString(i, str));
                        break;
                    }
                    case 0x0B:
                    case 0x33:
                    {
                        var str = ReadCString(opcode.Args);
                        list.Add(new TString(i, str));
                        break;
                    }
                }
            }

            return list;
        }
        public void ExportTextForTranslate(string filePath)
        {
            string chara;
            StringBuilder sb = new StringBuilder();
            StringBuilder rsb = new StringBuilder();
            StringBuilder rsb2 = new StringBuilder();
            using (var writer = File.CreateText(filePath))
            {
                foreach (var it in scripts.Commands)
                {
                    if (it.ContainsText)
                    {
                        chara = "";
                        sb.Clear();
                        rsb.Clear();
                        rsb2.Clear();
                        for (int i = 0; i < it.Sectors.Count; ++i)
                        {
                            
                            if (it.Sectors[i].IsText)
                            {
                                if (it.Sectors[i].TextType == 5)
                                {
                                    Debug.Assert(chara == "");
                                    chara = EscapeString(ReadCString(it.Sectors[i].Opcodes[0].Args));
                                }
                                else
                                {
                                    for(int j = 0; j < it.Sectors[i].Opcodes.Count; ++j)
                                    {
                                        if(it.Sectors[i].Opcodes[j].Code == 0x0A)
                                        {
                                            sb.Append(EscapeString(ReadCompressedString(it.Sectors[i].Opcodes[j].Args)));
                                        }
                                        else if(it.Sectors[i].Opcodes[j].Code == 0x1C)
                                        {
                                            if(it.Sectors[i].Opcodes[j].Args[0] == 0x00)
                                            {
                                                sb.Append('\n');
                                            }
                                            else if(it.Sectors[i].Opcodes[j].Args[0] == 0x01)
                                            {
                                                ++j;
                                                string ruby;
                                                do
                                                {
                                                    if(it.Sectors[i].Opcodes[j].Code == 0x0A)
                                                    {
                                                        ruby = EscapeString(ReadCompressedString(it.Sectors[i].Opcodes[j].Args));
                                                    }
                                                    else
                                                    {
                                                        ruby = EscapeString(ReadCString(it.Sectors[i].Opcodes[j].Args));
                                                    }
                                                    rsb.Append(ruby);
                                                    ++j;
                                                } while (it.Sectors[i].Opcodes[j].Code != 0x00);

                                                ++j;
                                                if (it.Sectors[i].Opcodes[j].Code == 0x0A)
                                                {
                                                    ruby = EscapeString(ReadCompressedString(it.Sectors[i].Opcodes[j].Args));
                                                }
                                                else
                                                {
                                                    ruby = EscapeString(ReadCString(it.Sectors[i].Opcodes[j].Args));
                                                }
                                                rsb2.Append(ruby);
                                                sb.Append($"{{{rsb2}:{rsb}}}");
                                            }
                                            else
                                            {
                                                throw new Exception("Unknown Escape Sequence");
                                            }
                                        }
                                        else if (it.Sectors[i].Opcodes[j].Code == 0x0B)
                                        {
                                            sb.Append(EscapeString(ReadCString(it.Sectors[i].Opcodes[j].Args)));
                                        }
                                    }
                                }
                            }
                        }
                        writer.WriteLine($"○{it.Index:D4}○{chara}○;----------------------------------------------------");
                        writer.WriteLine(sb.ToString());
                        writer.WriteLine($"●{it.Index:D4}●{chara}●");
                        writer.WriteLine(sb.ToString());
                    }
                }
                writer.Flush();
            }
             
        }
        public void ExportStrings(string filePath, bool exportAll)
        {
            var strings = GetStrings();

            using (var writer = File.CreateText(filePath))
            {
                foreach (var item in strings)
                {
                    if (!exportAll)
                    {
                        if (item.Item2.Length > 0 && item.Item2[0] < 0x81 && item.Item2[0] != '\n' && item.Item2[0] != '\r' && item.Item2[0] != '\t')
                        {
                            // Ignore file name, etc.
                            continue;
                        }
                    }

                    var str = EscapeString(item.Item2);

                    writer.WriteLine($"◇{item.Item1:X8}◇{str}");
                    writer.WriteLine($"◆{item.Item1:X8}◆{str}");
                    writer.WriteLine();
                }
            }
        }

        static string ReadCompressedString(byte[] buffer)
        {
            var bytes = new List<byte>(256);

            int i = 0;

            while (i < buffer.Length)
            {
                byte c = buffer[i++];

                if (c == 0)
                {
                    break;
                }
                if (c >= 0x81 && c <= 0x9F || (c + 0x20) <= 0xF)
                {
                    bytes.Add(c);
                    c = buffer[i++];
                    bytes.Add(c);
                }
                else
                {
                    // Unpack
                    var v0 = (ushort)(c - 0x7D62);
                    var v1 = (byte)(v0 >> 8);
                    var v2 = (byte)(v0 & 0xFF);
                    bytes.Add(v1);
                    bytes.Add(v2);
                }
            }

            if (bytes.Count == 0)
            {
                return string.Empty;
            }

            return _encoding.GetString(bytes.ToArray());
        }

        static string ReadCString(byte[] buffer)
        {
            var bytes = buffer.TakeWhile(c => c != 0).ToArray();
            return _encoding.GetString(bytes);
        }

        static string EscapeString(string input)
        {
            input = input.Replace("\r", "\\r");
            input = input.Replace("\n", "\\n");
            input = input.Replace("\t", "\\t");

            return input;
        }

        static string UnespaceString(string input)
        {
            input = input.Replace("\\r", "\r");
            input = input.Replace("\\n", "\n");
            input = input.Replace("\\t", "\t");
            input = input.Replace("\\0", "\0");
            input = input.Replace("\\x1C", "\x1C");

            return input;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Hessian.Collections;
using Hessian.Platform;

namespace Hessian
{
    public class Deserializer
    {
        private readonly ValueReader reader;
        private readonly IRefMap<ClassDef> classDefs;
        private readonly IRefMap<object> objectRefs;
        private readonly IRefMap<string> typeNameRefs;
        private readonly Lazy<ListTypeResolver> listTypeResolver = new Lazy<ListTypeResolver>();
        private readonly Lazy<DictionaryTypeResolver> dictTypeResolver = new Lazy<DictionaryTypeResolver>();

        private static readonly EndianBitConverter BitConverter = new BigEndianBitConverter();

        public Deserializer(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            reader = new ValueReader(stream);
            classDefs = new ListRefMap<ClassDef>();
            objectRefs = new ListRefMap<object>();
            typeNameRefs = new ListRefMap<string>();
        }

        public bool CanRead()
        {
            var tag = reader.Peek();
            return tag.HasValue;
        }

        #region ReadValue

        public object ReadValue()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }
            //Console.WriteLine("------------ 0x{0:x2}----------------",tag.Value);

            switch (tag.Value)
            {
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0A:
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                case 0x1D:
                case 0x1E:
                case 0x1F:
                    //Console.WriteLine("ReadShortString");
                    return ReadShortString();

                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                case 0x2F:
                    //Console.WriteLine("ReadShortBinary");
                    return ReadShortBinary();

                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                    //Console.WriteLine("ReadMediumString");
                    return ReadMediumString();

                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                    //Console.WriteLine("ReadMediumBinary");
                    return ReadMediumBinary();

                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                case 0x3D:
                case 0x3E:
                case 0x3F:
                    //Console.WriteLine("ReadLongThreeBytes");
                    return ReadLongThreeBytes();

                case 0x40:
                    //Console.WriteLine("Reserved");
                    return Reserved();

                case 0x41:
                case 0x42:
                    //Console.WriteLine("ReadChunkedBinary");
                    return ReadChunkedBinary();

                case 0x43:
                    //Console.WriteLine("ReadClassDefinition");
                    return ReadClassDefinition();

                case 0x44:
                    //Console.WriteLine("ReadFullDouble");
                    return ReadFullDouble();

                case 0x45:
                    //Console.WriteLine("Reserved");
                    return Reserved();

                case 0x46:
                    //Console.WriteLine("ReadBoolean");
                    return ReadBoolean();

                case 0x47:
                    //Console.WriteLine("Reserved");
                    return Reserved();

                case 0x48:
                    //Console.WriteLine("ReadUntypedMap");
                    return ReadUntypedMap();

                case 0x49:
                    //Console.WriteLine("ReadInteger");
                    return ReadInteger();

                case 0x4A:
                    //Console.WriteLine("ReadDateInMillis");
                    return ReadDateInMillis();

                case 0x4B:
                    //Console.WriteLine("ReadDateInMinutes");
                    return ReadDateInMinutes();

                case 0x4C:
                    //Console.WriteLine("ReadLongFull");
                    return ReadLongFull();

                case 0x4D:
                    //Console.WriteLine("ReadTypedMap");
                    return ReadTypedMap();

                case 0x4E:
                    //Console.WriteLine("ReadNull");
                    return ReadNull();

                case 0x4F:
                    //Console.WriteLine("ReadObject");
                    return ReadObject();

                case 0x50:
                    //Console.WriteLine("Reserved");
                    return Reserved();

                case 0x51:
                    //Console.WriteLine("ReadRef");
                    return ReadRef();

                case 0x52:
                case 0x53:
                    //Console.WriteLine("ReadChunkedString");
                    return ReadChunkedString();

                case 0x54:
                    //Console.WriteLine("ReadBoolean");
                    return ReadBoolean();

                case 0x55:
                    //Console.WriteLine("ReadVarList");
                    return ReadVarList();

                case 0x56:
                    //Console.WriteLine("ReadFixList");
                    return ReadFixList();

                case 0x57:
                    //Console.WriteLine("ReadVarListUntyped");
                    return ReadVarListUntyped();

                case 0x58:
                    //Console.WriteLine("ReadFixListUntyped");
                    return ReadFixListUntyped();

                case 0x59:
                    //Console.WriteLine("ReadLongFourBytes");
                    return ReadLongFourBytes();

                case 0x5A:
                    // List terminator - solitary list terminators are most definitely not legit.
                    throw new UnexpectedTagException(0x5A, "value");

                case 0x5B:
                case 0x5C:
                    //Console.WriteLine("ReadDoubleOneByte");
                    return ReadDoubleOneByte();

                case 0x5D:
                    //Console.WriteLine("ReadDoubleOneByte");
                    return ReadDoubleOneByte();

                case 0x5E:
                    //Console.WriteLine("ReadDoubleTwoBytes");
                    return ReadDoubleTwoBytes();

                case 0x5F:
                    //Console.WriteLine("ReadDoubleFourBytes");
                    return ReadDoubleFourBytes();

                case 0x60:
                case 0x61:
                case 0x62:
                case 0x63:
                case 0x64:
                case 0x65:
                case 0x66:
                case 0x67:
                case 0x68:
                case 0x69:
                case 0x6A:
                case 0x6B:
                case 0x6C:
                case 0x6D:
                case 0x6E:
                case 0x6F:
                    //Console.WriteLine("ReadObjectCompact");
                    return ReadObjectCompact();

                case 0x70:
                case 0x71:
                case 0x72:
                case 0x73:
                case 0x74:
                case 0x75:
                case 0x76:
                case 0x77:
                    //Console.WriteLine("ReadCompactFixList");
                    return ReadCompactFixList();

                case 0x78:
                case 0x79:
                case 0x7A:
                case 0x7B:
                case 0x7C:
                case 0x7D:
                case 0x7E:
                case 0x7F:
                    //Console.WriteLine("ReadCompactFixListUntyped");
                    return ReadCompactFixListUntyped();

                case 0x80:
                case 0x81:
                case 0x82:
                case 0x83:
                case 0x84:
                case 0x85:
                case 0x86:
                case 0x87:
                case 0x88:
                case 0x89:
                case 0x8A:
                case 0x8B:
                case 0x8C:
                case 0x8D:
                case 0x8E:
                case 0x8F:
                case 0x90:
                case 0x91:
                case 0x92:
                case 0x93:
                case 0x94:
                case 0x95:
                case 0x96:
                case 0x97:
                case 0x98:
                case 0x99:
                case 0x9A:
                case 0x9B:
                case 0x9C:
                case 0x9D:
                case 0x9E:
                case 0x9F:
                case 0xA0:
                case 0xA1:
                case 0xA2:
                case 0xA3:
                case 0xA4:
                case 0xA5:
                case 0xA6:
                case 0xA7:
                case 0xA8:
                case 0xA9:
                case 0xAA:
                case 0xAB:
                case 0xAC:
                case 0xAD:
                case 0xAE:
                case 0xAF:
                case 0xB0:
                case 0xB1:
                case 0xB2:
                case 0xB3:
                case 0xB4:
                case 0xB5:
                case 0xB6:
                case 0xB7:
                case 0xB8:
                case 0xB9:
                case 0xBA:
                case 0xBB:
                case 0xBC:
                case 0xBD:
                case 0xBE:
                case 0xBF:
                    //Console.WriteLine("ReadIntegerSingleByte");
                    return ReadIntegerSingleByte();

                case 0xC0:
                case 0xC1:
                case 0xC2:
                case 0xC3:
                case 0xC4:
                case 0xC5:
                case 0xC6:
                case 0xC7:
                case 0xC8:
                case 0xC9:
                case 0xCA:
                case 0xCB:
                case 0xCC:
                case 0xCD:
                case 0xCE:
                case 0xCF:
                    //Console.WriteLine("ReadIntegerTwoBytes");
                    return ReadIntegerTwoBytes();

                case 0xD0:
                case 0xD1:
                case 0xD2:
                case 0xD3:
                case 0xD4:
                case 0xD5:
                case 0xD6:
                case 0xD7:
                    //Console.WriteLine("ReadIntegerThreeBytes");
                    return ReadIntegerThreeBytes();

                case 0xD8:
                case 0xD9:
                case 0xDA:
                case 0xDB:
                case 0xDC:
                case 0xDD:
                case 0xDE:
                case 0xDF:
                case 0xE0:
                case 0xE1:
                case 0xE2:
                case 0xE3:
                case 0xE4:
                case 0xE5:
                case 0xE6:
                case 0xE7:
                case 0xE8:
                case 0xE9:
                case 0xEA:
                case 0xEB:
                case 0xEC:
                case 0xED:
                case 0xEE:
                case 0xEF:
                    //Console.WriteLine("ReadLongOneByte");
                    return ReadLongOneByte();

                case 0xF0:
                case 0xF1:
                case 0xF2:
                case 0xF3:
                case 0xF4:
                case 0xF5:
                case 0xF6:
                case 0xF7:
                case 0xF8:
                case 0xF9:
                case 0xFA:
                case 0xFB:
                case 0xFC:
                case 0xFD:
                case 0xFE:
                case 0xFF:
                    //Console.WriteLine("ReadLongTwoBytes");
                    return ReadLongTwoBytes();
            }

            throw new Exception("WTF: byte value " + tag.Value + " not accounted for!");
        }

        #endregion ReadValue

        private string ReadTypeName()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            // A type name is either a string, or an integer reference to a
            // string already read and stored in the type-name ref map.
            if ((tag >= 0x00 && tag < 0x20)
                || (tag >= 0x30 && tag < 0x34)
                || tag == 0x52
                || tag == 0x53)
            {
                var typeName = ReadString();
                //Console.WriteLine("Type={0}",typeName);
                typeNameRefs.Add(typeName);
                return typeName;
            }

            return typeNameRefs.Get(ReadInteger());
        }

        #region List

        private IList<object> ReadVarList()
        {
            reader.ReadByte();
            var type = ReadTypeName();
            return ReadListCore(type: type);
        }

        private IList<object> ReadFixList()
        {
            reader.ReadByte();
            var type = ReadTypeName();
            var length = ReadInteger();
            return ReadListCore(length, type);
        }

        private IList<object> ReadVarListUntyped()
        {
            reader.ReadByte();
            return ReadListCore();
        }

        private IList<object> ReadFixListUntyped()
        {
            reader.ReadByte();
            var length = ReadInteger();
            return ReadListCore(length);
        }

        private IList<object> ReadCompactFixList()
        {
            var tag = reader.ReadByte();
            var length = tag - 0x70;
            var type = ReadTypeName();
            return ReadListCore(length, type);
        }

        private IList<object> ReadCompactFixListUntyped()
        {
            var tag = reader.ReadByte();
            var length = tag - 0x70;
            return ReadListCore(length);
        }

        private IList<object> ReadListCore(int? length = null, string type = null)
        {
            var list = GetListInstance(type, length);

            objectRefs.Add(list);

            if (length.HasValue)
            {
                PopulateFixLengthList(list, length.Value);
            }
            else
            {
                PopulateVarList(list);
            }
            return list;
        }

        private IList<object> GetListInstance(string type, int? length = null)
        {
            IList<object> list;

            if (length.HasValue)
            {
                if (!listTypeResolver.Value.TryGetListInstance(type, length.Value, out list))
                {
                    list = new List<object>(length.Value);
                }
            }
            else
            {
                if (!listTypeResolver.Value.TryGetListInstance(type, out list))
                {
                    list = new List<object>();
                }
            }

            return list;
        }

        private void PopulateFixLengthList(IList<object> list, int length)
        {
            //  NOTE： 这里有BUG 第一层 如果是复杂对象 那第一个readValue的
            //  结果是 classDef,下一个才是真实的数据，但是如果数据的字段又是复杂类型
            //  下一个 就还是classDef 接着是真实数据，以此类型，但是呢，如果类型之前
            for (var i = 0; i < length; ++i)
            {
                var obj = ReadValue();
                switch (obj)
                {
                    case ClassDef _:
                        {
                            var rVal = ReadValue() as HessianObject; // 真实的数据
                            if (rVal == null)
                            {
                                throw new HessianException("decode error");
                            }
                            list.Add(ReadRealObject(rVal));
                            break;
                        }
                    case HessianObject vObj:
                        list.Add(ReadRealObject(vObj));
                        break;

                    default:
                        list.Add(obj);
                        break;
                }
            }
        }

        private HessianObject ReadRealObject(HessianObject hessianObject)
        {
            var builder = HessianObject.Builder.New(hessianObject.TypeName);
            foreach (var (k, v) in hessianObject)
            {
                if (v is ClassDef)
                {
                    if (!(ReadValue() is HessianObject rVal))
                    {
                        throw new HessianException("decode error");
                    }

                    builder.Add(k, ReadRealObject(rVal));
                }
                else if (v is HessianObject vObj)
                {
                    builder.Add(k, ReadRealObject(vObj));
                }
                else
                {
                    builder.Add(k, v);
                }
            }

            return builder.Create();
        }

        private void PopulateVarList(IList<object> list)
        {
            while (true)
            {
                var tag = reader.Peek();
                if (!tag.HasValue)
                {
                    throw new EndOfStreamException();
                }
                if (tag == 'Z')
                {
                    reader.ReadByte();
                    break;
                }
                list.Add(ReadValue());
            }
        }

        #endregion List

        public object Reserved()
        {
            reader.ReadByte();
            return ReadValue();
        }

        #region String

        public string ReadString()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            if (tag.Value < 0x20)
            {
                return ReadShortString();
            }

            if (tag.Value >= 0x30 && tag.Value <= 0x33)
            {
                return ReadMediumString();
            }

            if (tag.Value == 'R' || tag.Value == 'S')
            {
                return ReadChunkedString();
            }

            throw new UnexpectedTagException(tag.Value, "string");
        }

        private string ReadShortString()
        {
            var length = reader.ReadByte();
            return ReadStringWithLength(length);
        }

        private string ReadMediumString()
        {
            var b0 = reader.ReadByte();
            var b1 = reader.ReadByte();
            var length = ((b0 - 0x30) << 8) | b1;
            return ReadStringWithLength(length);
        }

        private string ReadStringWithLength(int length)
        {
            var sb = new StringBuilder(length);
            while (length-- > 0)
            {
                sb.AppendCodepoint(reader.ReadUtf8Codepoint());
            }
            return sb.ToString();
        }

        private string ReadChunkedString()
        {
            var sb = new StringBuilder();
            var final = false;

            while (!final)
            {
                var tag = reader.ReadByte();
                final = tag == 'S';
                var length = reader.ReadShort();
                while (length-- > 0)
                {
                    sb.AppendCodepoint(reader.ReadUtf8Codepoint());
                }
            }

            return sb.ToString();
        }

        #endregion String

        #region Binary

        public byte[] ReadBinary()
        {
            var tag = reader.Peek();
            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            if (tag.Value >= 0x20 && tag.Value <= 0x2F)
            {
                return ReadShortBinary();
            }

            if (tag.Value >= 0x34 && tag.Value <= 0x37)
            {
                return ReadMediumBinary();
            }

            if (tag.Value == 0x41 || tag.Value == 0x42)
            {
                return ReadChunkedBinary();
            }

            throw new UnexpectedTagException(tag.Value, "binary");
        }

        private byte[] ReadShortBinary()
        {
            var length = reader.ReadByte();
            var data = new byte[length];
            reader.Read(data, length);
            return data;
        }

        private byte[] ReadMediumBinary()
        {
            var b0 = reader.ReadByte();
            var b1 = reader.ReadByte();
            var length = ((b0 - 0x34) << 8) | b1;
            var data = new byte[length];
            reader.Read(data, length);
            return data;
        }

        public byte[] ReadChunkedBinary()
        {
            var data = new List<byte>();
            var final = false;

            while (!final)
            {
                var tag = reader.ReadByte();
                final = tag == 'B';
                var length = reader.ReadShort();
                var buff = new byte[length];
                reader.Read(buff, length);
                data.AddRange(buff);
            }

            return data.ToArray();
        }

        #endregion Binary

        #region Integer

        public int ReadInteger()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            // Full-length integer encoding is 'I' b0 b1 b2 b3 - i.e. a full 32-bit integer in big-endian order.
            if (tag == 0x49)
            {
                return ReadIntegerFull();
            }

            // Ints between -16 and 47 are encoded as value + 0x90.
            if (tag >= 0x80 && tag <= 0xBF)
            {
                return ReadIntegerSingleByte();
            }

            // Ints between -2048 and 2047 can be encoded as two octets with the leading byte from 0xC0 to 0xCF.
            if (tag >= 0xC0 && tag <= 0xCF)
            {
                return ReadIntegerTwoBytes();
            }

            // Ints between -262144 and 262143 can be three bytes with the first from 0xD0 to 0xD7.
            if (tag >= 0xD0 && tag <= 0xD7)
            {
                return ReadIntegerThreeBytes();
            }

            throw new UnexpectedTagException(tag.Value, "integer");
        }

        private int ReadIntegerFull()
        {
            reader.ReadByte(); // Discard tag.
            byte b0 = reader.ReadByte(),
                 b1 = reader.ReadByte(),
                 b2 = reader.ReadByte(),
                 b3 = reader.ReadByte();

            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        private int ReadIntegerSingleByte()
        {
            return reader.ReadByte() - 0x90;
        }

        private int ReadIntegerTwoBytes()
        {
            byte b0 = reader.ReadByte(),
                 b1 = reader.ReadByte();

            return ((b0 - 0xC8) << 8) | b1;
        }

        private int ReadIntegerThreeBytes()
        {
            byte b0 = reader.ReadByte(),
                 b1 = reader.ReadByte(),
                 b2 = reader.ReadByte();

            return ((b0 - 0xD4) << 16) | (b1 << 8) | b2;
        }

        #endregion Integer

        #region Class Definition

        public ClassDef ReadClassDefinition()
        {
            var tag = reader.ReadByte();
            if (tag != 'C')
            {
                throw new UnexpectedTagException(tag, "classdef");
            }
            var name = ReadString();
            var fieldCount = ReadInteger();
            var fields = new string[fieldCount];
            for (var i = 0; i < fields.Length; ++i)
            {
                fields[i] = ReadString();
            }

            var classDef = new ClassDef(name, fields);

            classDefs.Add(classDef);

            return classDef;
        }

        #endregion Class Definition

        #region Double

        public double ReadDouble()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            if (tag == 0x44)
            {
                return ReadFullDouble();
            }

            if (tag == 0x5B || tag == 0x5C)
            {
                return ReadDoubleOneByte();
            }

            if (tag == 0x5D)
            {
                return ReadDoubleTwoBytes();
            }

            if (tag == 0x5E)
            {
                return ReadDoubleThreeBytes();
            }

            if (tag == 0x5F)
            {
                return ReadDoubleFourBytes();
            }

            throw new UnexpectedTagException(tag.Value, "double");
        }

        private double ReadFullDouble()
        {
            var data = new byte[9]; // 9 bytes: tag + IEEE 8-byte double
            reader.Read(data, data.Length);
            return BitConverter.ToDouble(data, 1);
        }

        private double ReadDoubleOneByte()
        {
            // 0x5B encodes the double value 0.0, and 0x5C encodes 1.0.
            return reader.ReadByte() - 0x5B;
        }

        private double ReadDoubleTwoBytes()
        {
            // Doubles representing integral values between -128.0 and 127.0 are
            // encoded as single bytes.  Java bytes are signed, .NET bytes aren't,
            // so we have to cast it first.
            reader.ReadByte();
            return (sbyte)reader.ReadByte();
        }

        private double ReadDoubleThreeBytes()
        {
            // Doubles representing integral values between -32768.0 and 32767.0 are
            // encoded as two-byte integers.
            reader.ReadByte();
            return reader.ReadShort();
        }

        private double ReadDoubleFourBytes()
        {
            // Doubles that can be represented as singles are thusly encoded.
            var data = new byte[5];
            reader.Read(data, data.Length);
            return BitConverter.ToSingle(data, 0);
        }

        #endregion Double

        public bool ReadBoolean()
        {
            var tag = reader.ReadByte();

            switch (tag)
            {
                case 0x46: return false;
                case 0x54: return true;
            }

            throw new UnexpectedTagException(tag, "boolean");
        }

        #region Date

        public DateTime ReadDate()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            if (tag == 0x4A)
            {
                return ReadDateInMillis();
            }

            if (tag == 0x4B)
            {
                return ReadDateInMinutes();
            }

            throw new UnexpectedTagException(tag.Value, "date");
        }

        private DateTime ReadDateInMillis()
        {
            var data = new byte[9];
            reader.Read(data, data.Length);
            var millis = LongFromBytes(data, 1);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(millis);
        }

        private DateTime ReadDateInMinutes()
        {
            var data = new byte[5];
            reader.Read(data, data.Length);
            var minutes = IntFromBytes(data, 1);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(minutes);
        }

        #endregion Date

        #region Long

        public long ReadLong()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            if (tag == 0x4C)
            {
                return ReadLongFull();
            }

            if (tag >= 0xD8 && tag <= 0xEF)
            {
                return ReadLongOneByte();
            }

            if (tag >= 0xF0 && tag <= 0xFF)
            {
                return ReadLongTwoBytes();
            }

            if (tag >= 0x38 && tag <= 0x3F)
            {
                return ReadLongThreeBytes();
            }

            if (tag == 0x59)
            {
                return ReadLongFourBytes();
            }

            throw new UnexpectedTagException(tag.Value, "long");
        }

        private long ReadLongFull()
        {
            var data = new byte[9];

            reader.Read(data, data.Length);
            return LongFromBytes(data, 1);
        }

        private long ReadLongOneByte()
        {
            return reader.ReadByte() - 0xE0;
        }

        private long ReadLongTwoBytes()
        {
            byte b0 = reader.ReadByte(),
                 b1 = reader.ReadByte();

            return ((b0 - 0xF8) << 8) | b1;
        }

        private long ReadLongThreeBytes()
        {
            byte b0 = reader.ReadByte(),
                 b1 = reader.ReadByte(),
                 b2 = reader.ReadByte();

            return ((b0 - 0x3C) << 16) | (b1 << 8) | b2;
        }

        private long ReadLongFourBytes()
        {
            var data = new byte[5];
            reader.Read(data, data.Length);
            return IntFromBytes(data, 1);
        }

        #endregion Long

        #region Dictionary/Map

        public IDictionary<object, object> ReadMap()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            if (tag == 'H')
            {
                return ReadUntypedMap();
            }

            if (tag == 'M')
            {
                return ReadTypedMap();
            }

            throw new UnexpectedTagException(tag.Value, "map");
        }

        private IDictionary<object, object> ReadUntypedMap()
        {
            reader.ReadByte();
            return ReadMapCore();
        }

        private IDictionary<object, object> ReadTypedMap()
        {
            reader.ReadByte();
            var typeName = ReadTypeName();
            return ReadMapCore(typeName);
        }

        private IDictionary<object, object> ReadMapCore(string type = null)
        {
            IDictionary<object, object> dictionary;
            if (type == null || !dictTypeResolver.Value.TryGetInstance("", out dictionary))
            {
                dictionary = new Dictionary<object, object>();
            }

            objectRefs.Add(dictionary);

            while (true)
            {
                var tag = reader.Peek();

                if (!tag.HasValue)
                {
                    throw new EndOfStreamException();
                }
                if (tag == 'Z')
                {
                    break;
                }

                var key = ReadValue();
                var value = ReadValue();
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        #endregion Dictionary/Map

        #region Object

        public object ReadObject()
        {
            var tag = reader.Peek();

            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }

            if (tag == 'O')
            {
                return ReadObjectFull();
            }

            if (tag >= 0x60 && tag < 0x70)
            {
                return ReadObjectCompact();
            }

            throw new UnexpectedTagException(tag.Value, "object");
        }

        private object ReadObjectFull()
        {
            reader.ReadByte();
            var classDefId = ReadInteger();
            var classDef = classDefs.Get(classDefId);
            return ReadObjectCore(classDef);
        }

        private object ReadObjectCompact()
        {
            var classDefId = reader.ReadByte() - 0x60;
            var classDef = classDefs.Get(classDefId);
            return ReadObjectCore(classDef);
        }

        private object ReadObjectCore(ClassDef classDef)
        {
            // XXX: This needs a better implementation - maybe, you know, constructing
            //      the requested type?
            var builder = HessianObject.Builder.New(classDef.Name);
            objectRefs.Add(builder.Object);

            foreach (var field in classDef.Fields)
            {
                builder.Add(field, ReadValue());
            }

            return builder.Create();
        }

        #endregion Object

        public object ReadNull()
        {
            reader.ReadByte();
            return null;
        }

        public object ReadRef()
        {
            var tag = reader.Peek();
            if (!tag.HasValue)
            {
                throw new EndOfStreamException();
            }
            if (tag != 0x51)
            {
                throw new UnexpectedTagException(tag.Value, "ref");
            }

            reader.ReadByte();//过滤tag

            return objectRefs.Get(ReadInteger());
        }

        private static int IntFromBytes(byte[] buffer, int offset)
        {
            return (buffer[offset + 0] << 0x18)
                 | (buffer[offset + 1] << 0x10)
                 | (buffer[offset + 2] << 0x08)
                 | (buffer[offset + 3] << 0x00);
        }

        private static long LongFromBytes(byte[] buffer, int offset)
        {
            /*
              var value = (long) reader.ReadByte() << 56;

            value |= ((long) reader.ReadByte() << 48);
            value |= ((long) reader.ReadByte() << 40);
            value |= ((long) reader.ReadByte() << 32);
            value |= ((long) reader.ReadByte() << 24);
            value |= ((long) reader.ReadByte() << 16);
            value |= ((long) reader.ReadByte() << 8);
            value |= (uint)reader.ReadByte();

            return value;
            */
            return ((long)buffer[offset + 0] << 0x38)
                   + ((long)buffer[offset + 1] << 0x30)
                   | ((long)buffer[offset + 2] << 0x28)
                   | ((long)buffer[offset + 3] << 0x20)
                   | ((long)buffer[offset + 4] << 0x18)
                   | ((long)buffer[offset + 5] << 0x10)
                   | ((long)buffer[offset + 6] << 0x08)
                   | ((uint)buffer[offset + 7] << 0x00);
        }
    }
}
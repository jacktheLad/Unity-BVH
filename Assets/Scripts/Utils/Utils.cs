using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using ZeroFormatter;


/// <summary>
/// Math
/// </summary>
public static class MathUtils
{
    public static Vector3 ClampV3(Vector3 v, Vector3 min, Vector3 max)
    {
        return new Vector3(Mathf.Clamp(v.x, min.x, max.x), Mathf.Clamp(v.y, min.y, max.y), Mathf.Clamp(v.z, min.z, max.z));
    }
    public static Vector3Int ClampV3Int(Vector3Int v, Vector3Int min, Vector3Int max)
    {
        return new Vector3Int(Mathf.Clamp(v.x, min.x, max.x), Mathf.Clamp(v.y, min.y, max.y), Mathf.Clamp(v.z, min.z, max.z));
    }

    public static Vector3 Multiply(this Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public static Vector4 Swizzle(Vector3 v3, float w = 0.0f)
    {
        return new Vector4(v3.x, v3.y, v3.z, w);
    }
}

public static class ExtensionUtils
{
    public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
    {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;
        return list;
    }
}

public static class IOUtils
{
    public static string ConvertToString(byte[] data)
    {
        return Encoding.UTF8.GetString(data, 0, data.Length);
    }

    public static string ConvertToString(byte[] data, Encoding encoding)
    {
        return encoding.GetString(data, 0, data.Length);
    }

    public static byte[] ConvertToByte(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    public static byte[] ConvertToByte(string str, Encoding encoding)
    {
        return encoding.GetBytes(str);
    }

    public static byte[] SerializeToBinary<T>(T obj)
    {
        return ZeroFormatterSerializer.Serialize(obj);
    }

    public static void SerializeToFile<T>(T obj, string path)
    {
        var bytes = SerializeToBinary(obj);
        File.WriteAllBytes(path, bytes);
    }

    public static byte[] SerializeToXml(object obj)
    {
        MemoryStream stream = new MemoryStream();
        XmlSerializer xs = new XmlSerializer(obj.GetType());
        xs.Serialize(stream, obj);

        byte[] data = stream.ToArray();
        stream.Close();

        return data;
    }

    public static T DeserializeWithBinary<T>(byte[] data)
    {
        return ZeroFormatterSerializer.Deserialize<T>(data);
    }

    public static T DeserializeWithFile<T>(string file)
    {
        var bytes = File.ReadAllBytes(file);
        return DeserializeWithBinary<T>(bytes);
    }

    public static T DeserializeWithXml<T>(byte[] data)
    {
        MemoryStream stream = new MemoryStream();
        stream.Write(data, 0, data.Length);
        stream.Position = 0;
        XmlSerializer xs = new XmlSerializer(typeof(T));
        object obj = xs.Deserialize(stream);

        stream.Close();

        return (T)obj;
    }
}

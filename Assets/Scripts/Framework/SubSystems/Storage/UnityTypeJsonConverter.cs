using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 把 Unity 数学类型写成分量对象，避免默认反射沿 normalized 等属性递归。
    /// </summary>
    public class UnityTypeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2)
                || objectType == typeof(Vector3)
                || objectType == typeof(Vector4)
                || objectType == typeof(Vector2Int)
                || objectType == typeof(Vector3Int)
                || objectType == typeof(Quaternion)
                || objectType == typeof(Color);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            switch (value)
            {
                case Vector2 vector2:
                    WriteFloat(writer, "x", vector2.x);
                    WriteFloat(writer, "y", vector2.y);
                    break;
                case Vector3 vector3:
                    WriteFloat(writer, "x", vector3.x);
                    WriteFloat(writer, "y", vector3.y);
                    WriteFloat(writer, "z", vector3.z);
                    break;
                case Vector4 vector4:
                    WriteFloat(writer, "x", vector4.x);
                    WriteFloat(writer, "y", vector4.y);
                    WriteFloat(writer, "z", vector4.z);
                    WriteFloat(writer, "w", vector4.w);
                    break;
                case Vector2Int vector2Int:
                    WriteInt(writer, "x", vector2Int.x);
                    WriteInt(writer, "y", vector2Int.y);
                    break;
                case Vector3Int vector3Int:
                    WriteInt(writer, "x", vector3Int.x);
                    WriteInt(writer, "y", vector3Int.y);
                    WriteInt(writer, "z", vector3Int.z);
                    break;
                case Quaternion quaternion:
                    WriteFloat(writer, "x", quaternion.x);
                    WriteFloat(writer, "y", quaternion.y);
                    WriteFloat(writer, "z", quaternion.z);
                    WriteFloat(writer, "w", quaternion.w);
                    break;
                case Color color:
                    WriteFloat(writer, "r", color.r);
                    WriteFloat(writer, "g", color.g);
                    WriteFloat(writer, "b", color.b);
                    WriteFloat(writer, "a", color.a);
                    break;
                default:
                    throw new JsonSerializationException($"不支持序列化 {value?.GetType().Name ?? "null"}。");
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                throw new JsonSerializationException($"{objectType.Name} 不能为 null。");

            JObject obj = JObject.Load(reader);
            if (objectType == typeof(Vector2))
                return new Vector2(ReadFloat(obj, "x"), ReadFloat(obj, "y"));
            if (objectType == typeof(Vector3))
                return new Vector3(ReadFloat(obj, "x"), ReadFloat(obj, "y"), ReadFloat(obj, "z"));
            if (objectType == typeof(Vector4))
                return new Vector4(ReadFloat(obj, "x"), ReadFloat(obj, "y"), ReadFloat(obj, "z"), ReadFloat(obj, "w"));
            if (objectType == typeof(Vector2Int))
                return new Vector2Int(ReadInt(obj, "x"), ReadInt(obj, "y"));
            if (objectType == typeof(Vector3Int))
                return new Vector3Int(ReadInt(obj, "x"), ReadInt(obj, "y"), ReadInt(obj, "z"));
            if (objectType == typeof(Quaternion))
                return new Quaternion(ReadFloat(obj, "x"), ReadFloat(obj, "y"), ReadFloat(obj, "z"), ReadFloat(obj, "w"));
            if (objectType == typeof(Color))
                return new Color(ReadFloat(obj, "r"), ReadFloat(obj, "g"), ReadFloat(obj, "b"), ReadFloat(obj, "a"));

            throw new JsonSerializationException($"不支持反序列化 {objectType.Name}。");
        }

        private static void WriteFloat(JsonWriter writer, string name, float value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        private static void WriteInt(JsonWriter writer, string name, int value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        private static float ReadFloat(JObject obj, string name)
        {
            JToken token = obj[name];
            return token?.Value<float>() ?? 0f;
        }

        private static int ReadInt(JObject obj, string name)
        {
            JToken token = obj[name];
            return token?.Value<int>() ?? 0;
        }
    }
}

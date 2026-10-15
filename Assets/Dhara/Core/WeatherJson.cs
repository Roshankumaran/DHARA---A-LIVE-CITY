using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dhara
{
    // A small, strict JSON reader keeps null and absent numbers distinct from zero.
    // No reflection serializer, runtime code generation, or external JSON dependency.
    public static class WeatherJson
    {
        public static Dictionary<string,object> Object(string json) => new Reader(json).Read() as Dictionary<string,object> ?? throw new FormatException("Expected JSON object");
        public static WeatherResponse Parse(string json)
        {
            var root=new Reader(json).Read() as Dictionary<string,object>;
            if(root==null) throw new FormatException("Expected weather object");
            var result=new WeatherResponse();
            if(root.TryGetValue("current",out var current) && current is Dictionary<string,object> c)
                result.current=new ApiValues { time=String(c,"time"),interval=Number(c,"interval"),temperature_2m=Number(c,"temperature_2m"),
                    rain=Number(c,"rain"),precipitation=Number(c,"precipitation"),wind_speed_10m=Number(c,"wind_speed_10m"),wind_direction_10m=Number(c,"wind_direction_10m"),
                    cloud_cover=Number(c,"cloud_cover"),relative_humidity_2m=Number(c,"relative_humidity_2m"),shortwave_radiation=Number(c,"shortwave_radiation") };
            if(root.TryGetValue("hourly",out var hourly) && hourly is Dictionary<string,object> h)
                result.hourly=new ApiHourly { time=Strings(h,"time"),temperature_2m=Numbers(h,"temperature_2m"),rain=Numbers(h,"rain"),precipitation=Numbers(h,"precipitation"),
                    wind_speed_10m=Numbers(h,"wind_speed_10m"),wind_direction_10m=Numbers(h,"wind_direction_10m"),cloud_cover=Numbers(h,"cloud_cover"),
                    relative_humidity_2m=Numbers(h,"relative_humidity_2m"),shortwave_radiation=Numbers(h,"shortwave_radiation") };
            return result;
        }
        static string String(Dictionary<string,object> data,string key) => data.TryGetValue(key,out var value) ? value as string : null;
        static double? Number(Dictionary<string,object> data,string key) => data.TryGetValue(key,out var value) && value is double number ? number : (double?)null;
        static double?[] Numbers(Dictionary<string,object> data,string key)
        {
            if(!data.TryGetValue(key,out var value) || !(value is List<object> list)) return null;
            var result=new double?[list.Count];for(int i=0;i<list.Count;i++)result[i]=list[i] is double number ? number : (double?)null;return result;
        }
        static string[] Strings(Dictionary<string,object> data,string key)
        {
            if(!data.TryGetValue(key,out var value) || !(value is List<object> list)) return null;
            var result=new string[list.Count];for(int i=0;i<list.Count;i++)result[i]=list[i] as string;return result;
        }
        sealed class Reader
        {
            readonly string text; int position; int depth;
            public Reader(string text) { this.text=text ?? throw new FormatException("Empty response"); if(text.Length>2000000)throw new FormatException("Response too large"); }
            void White() { while(position<text.Length && (text[position]==' ' || text[position]=='\r' || text[position]=='\n' || text[position]=='\t'))position++; }
            bool Eat(char c) { White();if(position<text.Length && text[position]==c){position++;return true;}return false; }
            void Need(char c) { if(!Eat(c))throw new FormatException("Expected '"+c+"' at "+position); }
            public object Read() { var result=Value();White();if(position!=text.Length)throw new FormatException("Trailing JSON content");return result; }
            object Value()
            {
                White();if(position>=text.Length || ++depth>64)throw new FormatException("Invalid JSON depth or unexpected end");
                object value;
                switch(text[position])
                {
                    case '{': value=Object();break;
                    case '[': value=Array();break;
                    case '"': value=Quoted();break;
                    case 'n': Literal("null");value=null;break;
                    case 't': Literal("true");value=true;break;
                    case 'f': Literal("false");value=false;break;
                    default: value=Numeric();break;
                }
                depth--;return value;
            }
            Dictionary<string,object> Object()
            {
                Need('{');var result=new Dictionary<string,object>();if(Eat('}'))return result;
                do { White();string key=Quoted();Need(':');if(result.ContainsKey(key))throw new FormatException("Duplicate JSON property");result.Add(key,Value());if(Eat('}'))return result;Need(','); } while(true);
            }
            List<object> Array()
            {
                Need('[');var result=new List<object>();if(Eat(']'))return result;
                do { result.Add(Value());if(Eat(']'))return result;Need(','); } while(true);
            }
            string Quoted()
            {
                Need('"');var result=new StringBuilder();
                while(position<text.Length)
                {
                    char c=text[position++];if(c=='"')return result.ToString();if(c<32)throw new FormatException("Control character in string");
                    if(c!='\\'){result.Append(c);continue;}
                    if(position>=text.Length)throw new FormatException("Truncated escape");
                    switch(text[position++])
                    {
                        case '"':result.Append('"');break;case '\\':result.Append('\\');break;case '/':result.Append('/');break;
                        case 'b':result.Append('\b');break;case 'f':result.Append('\f');break;case 'n':result.Append('\n');break;case 'r':result.Append('\r');break;case 't':result.Append('\t');break;
                        case 'u':
                            if(position+4>text.Length || !ushort.TryParse(text.Substring(position,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out var code))throw new FormatException("Invalid Unicode escape");
                            result.Append((char)code);position+=4;break;
                        default:throw new FormatException("Unknown JSON escape");
                    }
                }
                throw new FormatException("Unterminated string");
            }
            void Literal(string literal) { if(position+literal.Length>text.Length || text.Substring(position,literal.Length)!=literal)throw new FormatException("Invalid literal");position+=literal.Length; }
            void Digits() { int start=position;while(position<text.Length && text[position]>='0' && text[position]<='9')position++;if(start==position)throw new FormatException("Expected JSON digits"); }
            double Numeric()
            {
                int start=position;if(position<text.Length && text[position]=='-')position++;
                if(position<text.Length && text[position]=='0')position++;else Digits();
                if(position<text.Length && text[position]=='.'){position++;Digits();}
                if(position<text.Length && (text[position]=='e' || text[position]=='E')){position++;if(position<text.Length && (text[position]=='+' || text[position]=='-'))position++;Digits();}
                if(!double.TryParse(text.Substring(start,position-start),NumberStyles.Float,CultureInfo.InvariantCulture,out double number) || double.IsNaN(number) || double.IsInfinity(number))throw new FormatException("Invalid finite JSON number");
                return number;
            }
        }
    }
}

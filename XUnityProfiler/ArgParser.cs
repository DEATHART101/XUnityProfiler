using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace XArgParser
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShortName : Attribute
    {
        public ShortName(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DefaultValue : Attribute
    {
        public DefaultValue(string def)
        {
            Default = def;
        }

        public string Default { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HelpInfo : Attribute
    {
        public HelpInfo(string info)
        {
            Info = info;
        }

        public string Info { get; set; }
    }

    public static class ArgParser
    {
        #region Define

        public interface IArgParserBase
        {
            public bool CanParse(Type type);

            public object ParseString(Type toType, string str);
        }

        private class StringArgParser : IArgParserBase
        {
            public bool CanParse(Type type)
            {
                return type == typeof(string);
            }

            public object ParseString(Type toType, string str)
            {
                return str;
            }
        }

        private class IntArgParser : IArgParserBase
        {
            public bool CanParse(Type type)
            {
                return type == typeof(int);
            }

            public object ParseString(Type toType, string str)
            {
                int val;
                if (int.TryParse(str, out val))
                {
                    return val;
                }

                return null;
            }
        }

        private class FloatArgParser : IArgParserBase
        {
            public bool CanParse(Type type)
            {
                return type == typeof(float);
            }

            public object ParseString(Type toType, string str)
            {
                float val;
                if (float.TryParse(str, out val))
                {
                    return val;
                }

                return null;
            }
        }

        private class BoolArgParser : IArgParserBase
        {
            public bool CanParse(Type type)
            {
                return type == typeof(bool);
            }

            public object ParseString(Type toType, string str)
            {
                bool val;
                if (bool.TryParse(str, out val))
                {
                    return val;
                }

                return null;
            }
        }

        private class EnumArgParser : IArgParserBase
        {
            public bool CanParse(Type type)
            {
                return type.IsEnum;
            }

            public object ParseString(Type toType, string str)
            {
                return Enum.Parse(toType, str, true);
            }
        }

        public struct ArgParseSettings
        {
            public List<IArgParserBase> AdditionParsers;
        }

        private struct ArgInfo
        {
            public string FullName;
            public string ShortName;
            public object DefaultValue;
            public string HelpInfo;
            public bool IsBool;
            public FieldInfo FieldInfo;

            public bool CannotBeParsed;

            public bool IsEnum
            {
                get
                {
                    return FieldInfo.FieldType.IsEnum;
                }
            }

            public IEnumerable<string> EnumNames
            {
                get
                {
                    foreach (var value in Enum.GetValues(FieldInfo.FieldType))
                    {
                        yield return value.ToString();
                    }
                }
            }

            public void Set(object obj, object value)
            {
                FieldInfo.SetValue(obj, value);
            }

            public object Get(object obj)
            {
                return FieldInfo.GetValue(obj);
            }

            public void SetDefault(object obj)
            {
                if (DefaultValue == null)
                {
                    return;
                }

                Set(obj, DefaultValue);
            }
        }

        private struct ArgInfoList
        {
            public Type TargetType;
            public List<ArgInfo> ArgsList;

            public object BuildDefaultObject()
            {
                object result = Activator.CreateInstance(TargetType);
                foreach (var item in ArgsList)
                {
                    item.SetDefault(result);
                }

                return result;
            }

            public bool IsHelpParam(string paramString)
            {
                if (XHelper.StringUtils.IsEmpty(paramString))
                {
                    return false;
                }

                paramString = paramString.ToLower();
                return paramString == "-h" || paramString == "--help";
            }

            public string BuildHelpMessage()
            {
                string result = "\nArguments:\n";
                if (ArgsList != null && ArgsList.Count >= 0)
                {
                    foreach (var item in ArgsList)
                    {
                        if (item.CannotBeParsed)
                        {
                            result += $"\t{item.FieldInfo.Name} of type {item.FieldInfo.FieldType} cannot be parsed\n";
                        }
                        else if (item.IsEnum)
                        {

                            result += $"\t-{item.ShortName}, --{item.FullName} {item.HelpInfo} (default: \"{item.DefaultValue}\", available values are {{{string.Join(", ", item.EnumNames)}}})\n";
                        }
                        else
                        {
                            result += $"\t-{item.ShortName}, --{item.FullName} {item.HelpInfo} (default: \"{item.DefaultValue}\")\n";
                        }
                    }
                }
                result += "\t-?, -h, --help  ShowHelp\n";

                return result;
            }

            public void Add(ArgInfo info)
            {
                ArgsList.Add(info);
            }

            public ArgInfo? FindArgInfo(string paramString)
            {
                string paramName;

                bool fullName = paramString.StartsWith("--");
                if (!fullName && !paramString.StartsWith('-'))
                {
                    return null;
                }

                paramName = paramString.Substring(fullName ? 2 : 1);

                int count = ArgsList.Count;
                for (int i = 0; i < count; i++)
                {
                    ArgInfo result = ArgsList[i];
                    if (fullName)
                    {
                        if (XHelper.StringUtils.IsSame(paramName, result.FullName, false))
                        {
                            return result;
                        }
                    }
                    else
                    {
                        if (XHelper.StringUtils.IsSame(paramName, result.ShortName, false))
                        {
                            return result;
                        }
                    }
                }

                return null;
            }
        }

        private class ArgParsers
        {
            public Dictionary<Type, IArgParserBase> Parsers;

            public bool CanParse(Type parseType)
            {
                return Parsers != null && Parsers.ContainsKey(parseType);
            }

            public void Add<T>(IArgParserBase parser)
            {
                Add(typeof(T), parser);
            }

            public void Add(Type type, IArgParserBase parser)
            {
                Parsers[type] = parser;
            }

            public object Parse<T>(string value)
            {
                return Parse(typeof(T), value);
            }

            public object Parse(Type type, string value)
            {
                IArgParserBase parser;
                if (Parsers.TryGetValue(type, out parser))
                {
                    return parser.ParseString(type, value);
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        private static ArgParsers MakeParsers()
        {
            ArgParsers parsers = new ArgParsers();
            parsers.Parsers = new Dictionary<Type, IArgParserBase>();

            parsers.Add<int>(new IntArgParser());
            parsers.Add<string>(new StringArgParser());
            parsers.Add<float>(new FloatArgParser());
            parsers.Add<bool>(new BoolArgParser());

            return parsers;
        }

        private static bool TryAddParser(Type parseType, ArgParsers argParsers, IEnumerable<IArgParserBase> additionParsers)
        {
            if (argParsers.CanParse(parseType))
            {
                return true;
            }

            if (additionParsers != null)
            {
                foreach (var item in additionParsers)
                {
                    if (item.CanParse(parseType))
                    {
                        argParsers.Add(parseType, item);
                        return true;
                    }
                }
            }

            return false;
        }

        private static ArgInfo MakeArgInfo(FieldInfo fieldInfo, ArgParsers argParsers, IEnumerable<IArgParserBase> additionParsers)
        {
            Type type = fieldInfo.DeclaringType;

            ArgInfo argInfo = default;
            argInfo.FullName = fieldInfo.Name;
            argInfo.IsBool = fieldInfo.FieldType == typeof(bool);
            argInfo.FieldInfo = fieldInfo;

            if (!TryAddParser(fieldInfo.FieldType, argParsers, additionParsers))
            {
                argInfo.CannotBeParsed = true;
                return argInfo;
            }

            argInfo.CannotBeParsed = false;

            ShortName nameAttr = fieldInfo.GetCustomAttribute<ShortName>();
            if (nameAttr != null && !XHelper.StringUtils.IsEmpty(nameAttr.Name))
            {
                argInfo.ShortName = nameAttr.Name;
            }
            else
            {
                argInfo.ShortName = null;
            }

            DefaultValue defaultValAttr = fieldInfo.GetCustomAttribute<DefaultValue>();
            if (defaultValAttr != null && !XHelper.StringUtils.IsEmpty(defaultValAttr.Default))
            {
                argInfo.DefaultValue = argParsers.Parse(fieldInfo.FieldType, defaultValAttr.Default);
            }
            else
            {
                object argStruct = Activator.CreateInstance(type);
                object defaultValue = fieldInfo.GetValue(argStruct);
                argInfo.DefaultValue = defaultValue;
            }

            HelpInfo helpInfoAttr = fieldInfo.GetCustomAttribute<HelpInfo>();
            if (helpInfoAttr != null && !XHelper.StringUtils.IsEmpty(helpInfoAttr.Info))
            {
                argInfo.HelpInfo = helpInfoAttr.Info;
            }
            else
            {
                argInfo.HelpInfo = null;
            }

            return argInfo;
        }

        private static ArgInfoList ResolveType(Type type, ArgParsers argParsers, IEnumerable<IArgParserBase> additionParsers)
        {
            ArgInfoList result = default;
            result.TargetType = type;
            result.ArgsList = new List<ArgInfo>();

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int count = fields.Length;
            for (int i = 0; i < count; i++)
            {
                result.Add(MakeArgInfo(fields[i], argParsers, additionParsers));
            }

            return result;
        }

        private static bool IsNameArg(string str)
        {
            return str.StartsWith("-");
        }

        private static bool TryProccessArg(ArgInfo argInfo, string valueArg, ArgParsers parsers, object resultObject, out string problem)
        {
            if (argInfo.IsBool && valueArg == null)
            {
                argInfo.Set(resultObject, true);
                problem = null;
                return true;
            }

            if (XHelper.StringUtils.IsEmpty(valueArg))
            {
                // Remember that resultObject is already a DefaultObject
                problem = null;
                return true;
            }

            object val;
            Exception ex;

            try
            {
                val = parsers.Parse(argInfo.FieldInfo.FieldType, valueArg);
                ex = null;
            }
            catch (Exception e)
            {
                val = null;
                ex = e;
            }

            if (val == null)
            {
                if (argInfo.IsEnum)
                {
                    problem = $"{argInfo.FieldInfo.FieldType} can only be set with {{{string.Join(", ", argInfo.EnumNames)}}}, but got {valueArg}";
                }
                else
                {
                    problem = $"Cannot parse value:{valueArg} to type:{argInfo.FieldInfo.FieldType}: {ex}";
                }
                return false;
            }

            problem = null;
            argInfo.Set(resultObject, val);
            return true;
        }

        public static object TryParse(Type argType, out string helpMessage, IEnumerable<string> args, ArgParseSettings settings = default)
        {
            helpMessage = null;

            if (argType == null)
            {
                return null;
            }

            if (!argType.IsValueType)
            {
                throw new System.Exception("ArgType must be a Struct");
            }

            List<string> listArgs = new List<string>(args);
            if (listArgs.Count == 1 && listArgs[0].Contains(' '))
            {
                listArgs = new List<string>(listArgs[0].Split(' '));
            }

            // Merge settings
            ArgParsers parsers = MakeParsers();

            List <IArgParserBase> additionParsers = new List<IArgParserBase>()
            {
                new EnumArgParser(),
            };
            if (settings.AdditionParsers != null)
            {
                additionParsers.AddRange(settings.AdditionParsers);
            }

            ArgInfoList infoList = ResolveType(argType, parsers, additionParsers);
            object resultObject = infoList.BuildDefaultObject();

            for (int i = 0; i < listArgs.Count;)
            {
                string item = listArgs[i];

                if (infoList.IsHelpParam(item))
                {
                    helpMessage = infoList.BuildHelpMessage();
                    return null;
                }

                bool isNameArg = IsNameArg(item);
                bool isLast = i == listArgs.Count - 1;
                bool isNextNameArg = false;
                if (!isLast)
                {
                    isNextNameArg = IsNameArg(listArgs[i + 1]);
                }

                if (!isNameArg)
                {
                    helpMessage = $"Expecting an ArgName but got {item}";
                    return null;
                }

                ArgInfo? infoOut = infoList.FindArgInfo(item);
                if (infoOut == null)
                {
                    helpMessage = $"Cannot find Arg with name {item}";
                    return null;
                }

                string valueArg = null;
                if (!isLast)
                {
                    if (isNextNameArg)
                    {
                        i += 1;
                    }
                    else
                    {
                        valueArg = listArgs[i + 1];
                        i += 2;
                    }
                }
                else
                {
                    i += 1;
                }

                string outProblem;
                bool success = TryProccessArg(infoOut.Value, valueArg, parsers, resultObject, out outProblem);
                if (!success)
                {
                    helpMessage = $"Failed to assign value {valueArg} to Arg {item}: {outProblem}";
                    return null;
                }
            }

            return resultObject;
        }

        public static bool TryParse<T>(out T result, out string helpMessage, IEnumerable<string> args, ArgParseSettings settings = default)
            where T : struct
        {
            object obj = TryParse(typeof(T), out helpMessage, args, settings);
            if (obj == null)
            {
                result = default;
                return false;
            }

            result = (T)(obj);
            return true;
        }

        public static string DeParse(object param, ArgParseSettings settings = default)
        {
            Type argType = param.GetType();

            if (argType == null)
            {
                return null;
            }

            if (!argType.IsValueType)
            {
                throw new System.Exception("ArgType must be a Struct");
            }

            // Merge settings
            ArgParsers parsers = MakeParsers();
            List<IArgParserBase> additionParsers = new List<IArgParserBase>()
            {
                new EnumArgParser(),
            };
            if (settings.AdditionParsers != null)
            {
                additionParsers.AddRange(settings.AdditionParsers);
            }

            ArgInfoList infoList = ResolveType(argType, parsers, additionParsers);

            List<string> args = new List<string>();
            foreach (ArgInfo item in infoList.ArgsList)
            {
                object val = item.Get(param);
                string arg = $"--{item.FullName} {val}";
                args.Add(arg);
            }

            return string.Join(" ", args);
        }
    }
}


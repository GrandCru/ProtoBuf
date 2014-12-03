using System;
using SilentOrbit.Code;

namespace SilentOrbit.ProtocolBuffers
{
    static class MessageCode
    {
        public static void GenerateInterface(ProtoMessage m, CodeWriter cw, Options options)
        {
            //Do not generate class code for external classes
            if (m.OptionExternal)
            {
                cw.Comment("Written elsewhere");
                cw.Comment(m.OptionAccess + " " + m.OptionType + " " + m.CsType + " {}");
                return;
            }

            cw.Bracket(m.OptionAccess + " partial interface I" + m.CsType);
            GenerateInterfaceProperties(m, cw);
            cw.EndBracket();

            foreach (ProtoMessage sub in m.Messages.Values)
            {
                cw.WriteLine();
                GenerateInterface(sub, cw, options);
            }                
        }

        private static void GenerateInterfaceProperties(ProtoMessage m, CodeWriter cw)
        {
            foreach (Field f in m.Fields.Values)
            {
                if (f.OptionExternal)
                {
                    cw.WriteLine("//" + GenerateInterfaceProperty(f) + " // Implemented by user elsewhere");
                }
                else
                {
                    if (f.Comments != null)
                    {
                        cw.Summary(f.Comments);
                    }
                    cw.WriteLine(GenerateInterfaceProperty(f));
                }

            }
        }

        private static string GenerateInterfaceProperty(Field f)
        {
            string type = f.ProtoType.FullInterfaceType;
            if (f.OptionCodeType != null)
            {
                type = f.OptionCodeType;
            }
            if (f.Rule == FieldRule.Repeated)
            {
                type = "IEnumerable<" + type + ">";
            }
                
            if (f.ProtoType.FullCsType == f.ProtoType.FullInterfaceType && f.Rule != FieldRule.Repeated)
            {
                return type + " " + f.CsName + " { get; } ";
            }
            else
            {
                return type + " " + f.InterfaceName + " { get; } ";
            }
        }

        public static void GenerateClass(ProtoMessage m, CodeWriter cw, Options options)
        {
            //Do not generate class code for external classes
            if (m.OptionExternal)
            {
                cw.Comment("Written elsewhere");
                cw.Comment(m.OptionAccess + " " + m.OptionType + " " + m.CsType + " {}");
                return;
            }

            //Default class
            cw.Summary(m.Comments);
            cw.Bracket(m.OptionAccess + " partial " + m.OptionType + " " + m.CsType + (options.GenerateInterfaces ? (" : " + m.CsInterfaceType) : ""));

            GenerateEnums(m, cw);
            GenerateProperties(m, cw, options);

            //if(options.GenerateToString...
            // ...

            if (m.OptionPreserveUnknown)
            {
                cw.Summary("Values for unknown fields.");
                cw.WriteLine("public List<global::SilentOrbit.ProtocolBuffers.KeyValue> PreservedFields;");
                cw.WriteLine();
            }

            if (m.OptionTriggers)
            {
                cw.Comment("protected virtual void BeforeSerialize() {}");
                cw.Comment("protected virtual void AfterDeserialize() {}");
                cw.WriteLine();
            }

            foreach (ProtoMessage sub in m.Messages.Values)
            {
                cw.WriteLine();
                GenerateClass(sub, cw, options);

            }
            cw.EndBracket();
            return;
        }

        static void GenerateEnums(ProtoMessage m, CodeWriter cw)
        {
            foreach (ProtoEnum me in m.Enums.Values)
            {
                GenerateEnum(me, cw);
            }
        }

        public static void GenerateEnum(ProtoEnum me, CodeWriter cw)
        {
            cw.Bracket("public enum " + me.CsType);
            foreach (var epair in me.Enums)
            {
                cw.Summary(epair.Comment);
                cw.WriteLine(epair.Name + " = " + epair.Value + ",");
            }
            cw.EndBracket();
            cw.WriteLine();
        }

        /// <summary>
        /// Generates the properties.
        /// </summary>
        /// <param name='template'>
        /// if true it will generate only properties that are not included by default, because of the [generate=false] option.
        /// </param>
        static void GenerateProperties(ProtoMessage m, CodeWriter cw, Options options)
        {
            foreach (Field f in m.Fields.Values)
            {
                if (f.OptionExternal)
                    cw.WriteLine("//" + GenerateProperty(f) + " // Implemented by user elsewhere");
                else
                {
                    if (f.Comments != null)
                    {
                        cw.Summary(f.Comments);
                    }
                    if (options.GenerateInterfaces && (f.ProtoType.FullCsType != f.ProtoType.FullInterfaceType || f.Rule == FieldRule.Repeated))
                    {
                        cw.WriteLine();
                    }
                    cw.WriteLine(GenerateProperty(f));
                    if (options.GenerateInterfaces && (f.ProtoType.FullCsType != f.ProtoType.FullInterfaceType || f.Rule == FieldRule.Repeated))
                    {
                        cw.WriteLine(GeneratePropertyInterface(f));
                    }
                }

            }

            //Wire format field ID
#if DEBUG
            cw.Comment("ProtocolBuffers wire field id");
            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("public const int " + f.CsName + "FieldID = " + f.ID + ";");
            }
#endif
        }

        static string GenerateProperty(Field f)
        {
            string type = f.ProtoType.FullCsType;
            if (f.OptionCodeType != null)
                type = f.OptionCodeType;
            if (f.Rule == FieldRule.Repeated)
                type = "List<" + type + ">";

            if (f.OptionReadOnly)
                return f.OptionAccess + " readonly " + type + " " + f.CsName + " = new " + type + "();";
            else if (f.ProtoType is ProtoMessage && f.ProtoType.OptionType == "struct")
                return f.OptionAccess + " " + type + " " + f.CsName + ";";
            else
                return f.OptionAccess + " " + type + " " + f.CsName + " { get; set; }";
        }

        static string GeneratePropertyInterface(Field f)
        {
            string type = f.ProtoType.FullInterfaceType;
            if (f.OptionCodeType != null)
            {
                type = f.OptionCodeType;
            }
            if (f.Rule == FieldRule.Repeated)
            {
                type = "IEnumerable<" + type + ">";
                return f.OptionAccess + " " + type + " "+ f.InterfaceName + " { get { return " + f.CsName + ".ToArray(); } }";
            }
            else
            {
                return f.OptionAccess + " " + type + " " + f.InterfaceName + " { get { return " + f.CsName + "; } }";
            }                                
        }
    }
}

